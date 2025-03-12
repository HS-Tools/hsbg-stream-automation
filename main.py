import os
import re
import time
import random
from pathlib import Path
from twitch_integration import TwitchIntegration
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

# Read credentials from environment variables
CLIENT_ID = os.getenv("CLIENT_ID")
CLIENT_SECRET = os.getenv("CLIENT_SECRET")
OAUTH_TOKEN = os.getenv("OAUTH_TOKEN")
BROADCASTER_ID = os.getenv("BROADCASTER_ID")

# Use these credentials to initialize TwitchIntegration
twitch = TwitchIntegration(
    client_id=CLIENT_ID,
    client_secret=CLIENT_SECRET,
    oauth_token=OAUTH_TOKEN,
    broadcaster_id=BROADCASTER_ID
)

def find_latest_log_folder(log_base_path):
    """Find the latest modified folder in the Hearthstone Logs directory."""
    try:
        log_base = Path(log_base_path)
        latest_folder = max(
            (folder for folder in log_base.iterdir() if folder.is_dir()),
            key=lambda folder: folder.stat().st_mtime
        )
        return latest_folder
    except Exception as e:
        print(f"Error finding latest log folder: {e}")
        return None

def tail_log_file(log_file_path):
    """Continuously listens to a log file for new content."""
    try:
        with open(log_file_path, "r", encoding="utf-8") as log_file:
            log_file.seek(0, os.SEEK_END)  # Start at the end of the file
            while True:
                line = log_file.readline()
                if not line:  # No new line
                    time.sleep(0.1)
                    continue
                yield line.strip()  # Ensure the line is stripped of extra spaces/newlines
    except Exception as e:
        print(f"Error reading log file: {e}")
        yield None

def extract_final_placement(line, player_entity):
    """Extracts final placement if the player's entity is tagged."""
    if "TAG_CHANGE" in line and "PLAYER_LEADERBOARD_PLACE" in line:
        match = re.search(r'entity=(\S+).*PLAYER_LEADERBOARD_PLACE.*value=(\d+)', line)
        if match:
            entity = match.group(1)
            placement = int(match.group(2))
            if entity == player_entity:
                return placement
    return None

def extract_player_entity(line):
    """Extracts the player's entity ID from the log."""
    if "GameEntity" in line and "tag=PLAYER_ID" in line:
        match = re.search(r'GameEntity.*entity=(\S+)', line)
        if match:
            return match.group(1)
    return None

def monitor_battlegrounds_final_placement(log_file_path):
    print("Monitoring log file for final Battlegrounds placements...")
    hero = None
    rank = -1
    placement_threshold = -1

    try:
        for line in tail_log_file(log_file_path):
            if not line:
                continue
            

            # Close any existing predictions when the game starts
            if "CREATE_GAME" in line:
                game_active = True
                player_entity = None
                final_placement = None
                print("New game started.")
                
                # Fetch and cancel active predictions
                active_predictions = twitch.get_active_predictions()
                if active_predictions:
                    for prediction in active_predictions:
                        twitch.end_prediction(winning_outcome_title=None)  # Cancel the prediction
                        print(f"Closed existing prediction: {prediction['title']}")

            # Detect hero pick and create a prediction
            if line and "GameState.SendChoices()" in line and "cardId" in line and "m_chosenEntities[0]=[entityName=" in line and "zone=HAND" in line:
                startind = line.index("entityName=") + 11
                endind = line.index("id=", startind) - 1
                hero = line[startind:endind]
                print(f"HERO PICKED: {hero}")

                # Cancel any active predictions before creating a new one
                active_predictions = twitch.get_active_predictions()
                if active_predictions:
                    for prediction in active_predictions:
                        twitch.end_prediction(winning_outcome_title=None)  # Cancel the prediction
                        print(f"Closed existing prediction: {prediction['title']}")

                # Start a new prediction for this hero
                placement_threshold = random.choice([2, 2, 2, 3, 3, 3, 3, 4, 4])
                base_title = f"Will I get top {placement_threshold} with {hero}?"
                truncated_title = base_title[:45]
                twitch.create_prediction(
                    title=truncated_title,
                    outcomes=["Yes", "No"],
                    duration=120
                )
                print(f"Started prediction for hero: {hero}")


            if "PLAYER_LEADERBOARD_PLACE" in line and f"entityName={hero}" in line and "GameState" in line:
                rank = line[-1]
                print(f"RANK DETECTED: {rank}")

            # Finalize game and resolve prediction
            if line and "tag=STEP value=FINAL_GAMEOVER" in line and hero:
                print(f"Final RANK: {rank}")
                time.sleep(60)

                # Resolve the prediction
                winning_outcome = "Yes" if int(rank) <= placement_threshold else "No"
                twitch.end_prediction(winning_outcome_title=winning_outcome)
                twitch.run_ad(length=90)

                # Reset game state
                hero = None
                rank = -1
                print("GAME OVER")

    except FileNotFoundError:
        print(f"Power.log file not found at {log_file_path}.")
    except Exception as e:
        print(f"Error monitoring log file: {e}")


# Main script
log_base_path = r"C:\Program Files (x86)\Hearthstone\Logs"  # Hearthstone logs directory
latest_folder = find_latest_log_folder(log_base_path)

if latest_folder:
    log_file_path = latest_folder / "Power.log"
    if log_file_path.exists():
        monitor_battlegrounds_final_placement(log_file_path)
    else:
        print(f"Power.log not found in the latest log folder: {latest_folder}")
else:
    print("No log folders found.")
