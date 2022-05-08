using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Services
{
    public class SpotifyService
    {
        private string _clientID;
        private string _clientSecret;
        private SpotifyClient _spotify;
        private EmbedIOAuthServer _server;

        public async Task InitializeAsync(string clientID, string clientSecret)
        {
            Console.WriteLine($"++SpotifyService.InitializeAsync(), " +
                $"clientID.Length={clientID.Length}, clientSecret.Length={clientSecret.Length}");
            _clientID = clientID;
            _clientSecret = clientSecret;
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var loginReq = new LoginRequest(_server.BaseUri, clientID, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadEmail }
            };
            BrowserUtil.Open(loginReq.ToUri());
        }

        public async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            

            // Exchange our code response for a token
            var tokenResponse = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(
                    _clientID, _clientSecret, response.Code, new Uri("http://localhost:5000/callback")
                )
            );

            // This config uses AuthorizationCodeAuthenticator to take care of token refresh when necessary
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new AuthorizationCodeAuthenticator(_clientID, _clientSecret, tokenResponse));

            _spotify = new SpotifyClient(config);
            Console.WriteLine("SpotifyService.OnAuthorizationCodeReceived(): Spotify client logged in!");
            var albums = await _spotify.Library.GetAlbums();
            Console.WriteLine($"ALBUM: {albums.Items[0].Album.Name}");

            await _server.Stop();
        }

        public async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"SpotifyService authorization error: {error}");
            await _server.Stop();
        }
    }
}
