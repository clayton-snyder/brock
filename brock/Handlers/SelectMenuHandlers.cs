using brock.Services;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace brock.Handlers
{
    public class SelectMenuHandlers
    {
        private readonly DiscordSocketClient _socketClient;
        private readonly SpotifyService _spotify;
        private readonly ConfigService _config;

        public SelectMenuHandlers(DiscordSocketClient socketClient, SpotifyService spotify, ConfigService config)
        {
            _socketClient = socketClient;
            _spotify = spotify;
            _config = config;
        }

        public void Initialize()
        {
            Console.WriteLine("[[ SelectMenuHandler.Initialize() ]]");

            _socketClient.SelectMenuExecuted += SelectMenuExecuted;
        }

        private async Task SelectMenuExecuted(SocketMessageComponent arg)
        {
            string value = arg.Data.Values.FirstOrDefault();
            switch (arg.Data.CustomId)
            {
                case "search-results":
                    try
                    {
                        // Optimally we wouldn't do a second network call here but Menus don't allow you to pass more than a 100-char string as value
                        // and it's much cleaner than searching through the Context.
                        SpotifyAPI.Web.FullTrack track = await _spotify.GetTrackByUri(value);
                        await _spotify.QueueTrack(track.Uri);
                        await arg.RespondAsync($"{arg.User.Username} queued *{track.Name}* by {String.Join(", ", track.Artists.Select(a => a.Name))}.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e}");
                        await arg.RespondAsync($"Error queueing track: {e.Message}");
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown menu ID: {arg.Data.CustomId}");
                    await arg.RespondAsync($"I don't know what this menu is: {arg.Data.CustomId}");
                    break;
            }
        }
    }
}
