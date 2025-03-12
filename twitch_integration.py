import requests
import os
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

class TwitchIntegration:
    def __init__(self, client_id, client_secret, oauth_token, broadcaster_id):
        self.client_id = client_id
        self.client_secret = client_secret
        self.oauth_token = oauth_token
        self.broadcaster_id = broadcaster_id
        self.base_url = "https://api.twitch.tv/helix"

    def _headers(self):
        return {
            "Client-ID": self.client_id,
            "Authorization": f"Bearer {self.oauth_token}",
        }

    def create_prediction(self, title, outcomes, duration):
        """
        Creates a Twitch prediction with the given title, outcomes, and duration.
        """
        url = f"{self.base_url}/predictions"
        data = {
            "broadcaster_id": self.broadcaster_id,
            "title": title,
            "outcomes": [{"title": outcome} for outcome in outcomes],
            "prediction_window": duration,
        }
        response = requests.post(url, json=data, headers=self._headers())
        if response.status_code == 201:
            print("Prediction created successfully.")
            return response.json()["data"][0]["id"]  # Return prediction ID
        else:
            print(f"Failed to create prediction: {response.status_code}, {response.text}")
            return None
            
    def get_active_predictions(self):
        """
        Fetches active predictions for the broadcaster.
        Returns a list of active predictions.
        """
        url = f"{self.base_url}/predictions?broadcaster_id={self.broadcaster_id}"
        response = requests.get(url, headers=self._headers())
        
        if response.status_code != 200:
            print(f"Failed to fetch predictions: {response.status_code}, {response.text}")
            return []

        try:
            predictions = response.json().get("data", [])
            active_predictions = [p for p in predictions if p["status"] == "ACTIVE" or p["status"] == "LOCKED"]

            if not active_predictions:
                print("No active predictions found.")
            return active_predictions
        except Exception as e:
            print(f"Error parsing prediction data: {e}")
            return []


    def end_prediction(self, winning_outcome_title=None):
        """
        Ends or cancels a prediction. If `winning_outcome_title` is None, cancels the prediction.
        """
        url = f"{self.base_url}/predictions"
        current_prediction = self.get_active_predictions()[0] if len(self.get_active_predictions()) == 1 else None
        yes_id = current_prediction["outcomes"][0]["id"]
        no_id = current_prediction["outcomes"][1]["id"]
        winning_outcome_id = yes_id if winning_outcome_title == 'Yes' else no_id
        
        data = {
            "broadcaster_id": self.broadcaster_id,
            "id": current_prediction['id'] if current_prediction else None,
            "status": "CANCELED" if winning_outcome_title is None else "RESOLVED",
            "winning_outcome_id": winning_outcome_id,
        }

        response = requests.patch(url, json=data, headers=self._headers())
        if response.status_code == 200:
            print("Prediction updated successfully.")
        else:
            print(f"Failed to update prediction: {response.status_code}, {response.text}")


    def run_ad(self, length):
        """
        Runs an ad on the Twitch channel for the given length in seconds.
        """
        url = f"{self.base_url}/channels/commercial"
        data = {
            "broadcaster_id": self.broadcaster_id,
            "length": length,
        }
        response = requests.post(url, json=data, headers=self._headers())
        if response.status_code == 200:
            print("Ad started successfully.")
        else:
            print(f"Failed to run ad: {response.status_code}, {response.text}")

# # Read credentials from environment variables
# CLIENT_ID = os.getenv("CLIENT_ID")
# CLIENT_SECRET = os.getenv("CLIENT_SECRET")
# OAUTH_TOKEN = os.getenv("OAUTH_TOKEN")
# BROADCASTER_ID = os.getenv("BROADCASTER_ID")

# twitch = TwitchIntegration(
#     client_id=CLIENT_ID,
#     client_secret=CLIENT_SECRET,
#     oauth_token=OAUTH_TOKEN,
#     broadcaster_id=BROADCASTER_ID
# )

# twitch.end_prediction('Yes')