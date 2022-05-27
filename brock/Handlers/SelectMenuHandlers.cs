using brock.Services;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Handlers
{
    public class SelectMenuHandlers
    {
        private readonly DiscordSocketClient _socketClient;
        private readonly SpotifyService _spotify;

        public SelectMenuHandlers(DiscordSocketClient socketClient, SpotifyService spotify)
        {
            _socketClient = socketClient;
            _spotify = spotify;
        }

        public void Initialize()
        {
            _socketClient.SelectMenuExecuted += SelectMenuExecuted;
        }

        private async Task SelectMenuExecuted(SocketMessageComponent arg)
        {
            switch (arg.Data.CustomId)
            {
                case "search-results":
                    Console.WriteLine($"Received SelectMenuExecuted event: User={arg.User} Value={arg.Data.Value}");
                    try
                    {
                        await _spotify.QueueTrack(arg.Data.Values.First());
                        await arg.RespondAsync($"{arg.User.Username} queued a track.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e}");
                        await arg.RespondAsync($"Error queueing track: {e.Message}");
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown menu ID: {arg.Data.CustomId}");
                    break;
            }
        }
    }
}
