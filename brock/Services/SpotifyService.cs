﻿using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const string REDIRECT_URI = @"http://localhost:5000/brocktch";
        private string _clientID;
        private string _clientSecret;
        public SpotifyClient Client;
        private EmbedIOAuthServer _server;
        private readonly ConfigService _config;

        public SpotifyService(ConfigService config = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            _config = config;
        }

        public async Task InitializeAsync(string clientID, string clientSecret)
        {
            Console.WriteLine($"IN SPOTIFYSERVICE: KeyOne={_config.Get<int>("KeyOne")}");
            Console.WriteLine($"++SpotifyService.InitializeAsync(), " +
                $"clientID.Length={clientID.Length}, clientSecret.Length={clientSecret.Length}");
            _clientID = clientID;
            _clientSecret = clientSecret;
            _server = new EmbedIOAuthServer(new Uri(REDIRECT_URI), 5000);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var loginReq = new LoginRequest(_server.BaseUri, clientID, LoginRequest.ResponseType.Code)
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
                    _clientID, _clientSecret, response.Code, new Uri(REDIRECT_URI)
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


        /// Pre-wrapped helper methods
        public async Task<List<FullTrack>> QueryTracksByName(string trackName)
        {
            SearchRequest req = new SearchRequest(SearchRequest.Types.Track, trackName);
            SearchResponse res = await Client.Search.Item(req);
            return res.Tracks.Items;
        }

        public async Task QueueTrack(string trackUri)
        {
            await Client.Player.AddToQueue(new PlayerAddToQueueRequest(trackUri));
        }
    }
}
