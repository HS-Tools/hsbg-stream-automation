using System;

namespace TwitchPredictionTest
{
    public class TwitchConfig
    {
        public string ClientId { get; private set; }
        public string ClientSecret { get; private set; }
        public string OAuthToken { get; private set; }
        public string BroadcasterId { get; private set; }

        public TwitchConfig()
        {
            // NOTE: In production, these values should be stored securely and not in source code
            ClientId = "gp762nuuoqcoxypju8c569th9wz7q5";
            ClientSecret = "wbbpdopw746z6h0xmmonjwl3vbdw0k";
            OAuthToken = "zm8ah3e5lgidyhn8cqs0vyrr2yhszb";
            BroadcasterId = "73626243";
        }
    }
}