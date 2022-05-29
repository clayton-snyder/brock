using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace brock.Services
{
    /// <summary>
    /// Helper functions for interacting with Spotify API through the SpotifyAPI library. Use the pre-wrapped
    /// helper methods defined here first, but the actual client is exposed (as "Client") if necessary. This 
    /// class handles all of the auth and token request/refresh internally and should be used as a Singleton
    /// through the DI framework.
    /// </summary>
    public class SpotifyService
    {
        public SpotifyClient Client;
        private EmbedIOAuthServer _server;
        private readonly ConfigService _config;

        private string _clientID;
        private string _clientSecret;
        private string _redirectUri;

        public SpotifyService(ConfigService config = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            _config = config;
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("[[ SpotifyService.InitializeAsync() ]]");

            _clientID = _config.Get<string>("SpotifyClientID");
            _clientSecret = _config.Get<string>("SpotifyClientSecret");
            _redirectUri = _config.Get<string>("SpotifyRedirectURI");
            _server = new EmbedIOAuthServer(new Uri(_redirectUri), 5000);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var loginReq = new LoginRequest(_server.BaseUri, _clientID, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { 
                    Scopes.UserReadPlaybackState,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserLibraryRead,
                    Scopes.UserLibraryModify,
                    Scopes.UserFollowRead,
                    Scopes.UserFollowModify,
                    Scopes.UserReadRecentlyPlayed,
                }
            };
            BrowserUtil.Open(loginReq.ToUri());
        }

        public async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            // Exchange our code response for a token
            var tokenResponse = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(
                    _clientID, _clientSecret, response.Code, new Uri(_redirectUri)
                )
            );

            // This config uses AuthorizationCodeAuthenticator to take care of token refresh when necessary
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new AuthorizationCodeAuthenticator(_clientID, _clientSecret, tokenResponse));

            Client = new SpotifyClient(config);
            Console.WriteLine("SpotifyService.OnAuthorizationCodeReceived(): Spotify client logged in!");

            try {
                var albums = await Client.Library.GetAlbums();
                Console.WriteLine($"ALBUM: {albums.Items[0].Album.Name}");
            } catch (Exception ex) { Console.WriteLine($"EXCEPTION: {ex.Message}"); }
        }

        public async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"SpotifyService authorization error: {error}");
            await _server.Stop();
        }


        public async Task<List<FullTrack>> QueryTracksByName(string trackName, int max = 20)
        {
            max = max > 20 ? 20 : max;
            SearchRequest req = new SearchRequest(SearchRequest.Types.Track, trackName);
            SearchResponse res = await Client.Search.Item(req);
            if (res.Tracks.Items.Count > max)
            {
                return res.Tracks.Items.Take(max).ToList();
            }
            return res.Tracks.Items;
        }

        public async Task QueueTrack(string trackUri)
        {
            await Client.Player.AddToQueue(new PlayerAddToQueueRequest(trackUri));
        }

        public async Task<FullTrack> GetTrackByUri(string trackId)
        {
            return await Client.Tracks.Get(trackId);
        }
    }
}
