using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using HSBG_Ads_Predictions_for_Twitch.Properties;

namespace HSBG_Ads_Predictions_for_Twitch.Auth
{
    public class TwitchAuth
    {
        private const string ClientId = "YOUR_CLIENT_ID"; // You'll need to replace this with your Twitch app client ID
        private const string RedirectUri = "http://localhost:3000/oauth/callback";
        private const string Scopes = "channel:manage:predictions channel:manage:commercial";
        private HttpListener _httpListener;
        private string _state;
        private TaskCompletionSource<(string accessToken, string refreshToken)> _authCompletionSource;

        public event Action<string> OnAuthSuccess;
        public event Action<string> OnAuthError;

        public async Task<bool> StartAuthFlow()
        {
            try
            {
                _state = GenerateState();
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add("http://localhost:3000/oauth/callback/");
                _httpListener.Start();

                _authCompletionSource = new TaskCompletionSource<(string accessToken, string refreshToken)>();

                // Launch browser with auth URL
                var authUrl = $"https://id.twitch.tv/oauth2/authorize" +
                    $"?client_id={ClientId}" +
                    $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                    $"&response_type=code" +
                    $"&scope={Uri.EscapeDataString(Scopes)}" +
                    $"&state={_state}";

                System.Diagnostics.Process.Start(authUrl);

                // Start listening for the callback
                ListenForCallback();

                var (accessToken, refreshToken) = await _authCompletionSource.Task;
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Save the tokens
                    Settings.Default.AccessToken = $"Bearer {accessToken}";
                    Settings.Default.RefreshToken = refreshToken;
                    Settings.Default.Save();

                    // Get user info
                    var username = await GetUserInfo(accessToken);
                    OnAuthSuccess?.Invoke(username);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnAuthError?.Invoke($"Authentication failed: {ex.Message}");
                return false;
            }
            finally
            {
                StopListener();
            }
        }

        private async void ListenForCallback()
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                var response = context.Response;

                var query = HttpUtility.ParseQueryString(context.Request.Url.Query);
                var state = query["state"];
                var code = query["code"];
                var error = query["error"];

                if (error != null)
                {
                    SendResponse(response, "Authentication failed. You can close this window.");
                    _authCompletionSource.SetResult((null, null));
                    OnAuthError?.Invoke($"Authentication error: {error}");
                    return;
                }

                if (state != _state)
                {
                    SendResponse(response, "Invalid state parameter. Authentication failed. You can close this window.");
                    _authCompletionSource.SetResult((null, null));
                    OnAuthError?.Invoke("Invalid state parameter");
                    return;
                }

                // Exchange code for token
                var tokens = await ExchangeCodeForToken(code);
                SendResponse(response, "Authentication successful! You can close this window.");
                _authCompletionSource.SetResult(tokens);
            }
            catch (Exception ex)
            {
                _authCompletionSource.SetException(ex);
            }
        }

        private async Task<(string accessToken, string refreshToken)> ExchangeCodeForToken(string code)
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", ClientId),
                    new KeyValuePair<string, string>("client_secret", "YOUR_CLIENT_SECRET"), // Replace with your client secret
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", RedirectUri)
                });

                var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);
                var responseString = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseString);

                return (json["access_token"]?.ToString(), json["refresh_token"]?.ToString());
            }
        }

        private async Task<string> GetUserInfo(string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Client-ID", ClientId);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await client.GetAsync("https://api.twitch.tv/helix/users");
                var responseString = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseString);

                return json["data"]?[0]?["login"]?.ToString();
            }
        }

        private void SendResponse(HttpListenerResponse response, string message)
        {
            var buffer = Encoding.UTF8.GetBytes($@"
                <html>
                    <head>
                        <style>
                            body {{ 
                                font-family: Arial, sans-serif;
                                display: flex;
                                justify-content: center;
                                align-items: center;
                                height: 100vh;
                                margin: 0;
                                background-color: #f0f0f0;
                            }}
                            .message {{
                                padding: 20px;
                                background-color: white;
                                border-radius: 8px;
                                box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                                text-align: center;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='message'>
                            <h2>{message}</h2>
                        </div>
                    </body>
                </html>");

            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private string GenerateState()
        {
            var random = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(random);
            }
            return Convert.ToBase64String(random);
        }

        private void StopListener()
        {
            if (_httpListener?.IsListening == true)
            {
                _httpListener.Stop();
                _httpListener = null;
            }
        }

        public void Disconnect()
        {
            Settings.Default.AccessToken = "";
            Settings.Default.RefreshToken = "";
            Settings.Default.Save();
        }
    }
} 