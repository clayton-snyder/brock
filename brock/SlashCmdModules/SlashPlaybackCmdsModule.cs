using Discord;
using Discord.Interactions;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Services
{
    public class SlashPlaybackCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        public SpotifyService Spotify { get; set; }
        private readonly List<string> ackReplies = new List<string> { "OK!", "Got it!", "Yes!", "No problem!", "Done!" };
        private static readonly Random random = new Random();

        [SlashCommand("player", "Control the music playback.")]
        public async Task Player(
            [Choice("play", "play"),
            Choice("pause", "pause"), 
            Choice("skip", "skip"),
            Choice("back", "back")] string action)
        {
            try
            {
                switch (action)
                {
                    case "play":
                        await Spotify.Client.Player.ResumePlayback();
                        break;
                    case "pause":
                        await Spotify.Client.Player.PausePlayback();
                        break;
                    case "skip":
                        await Spotify.Client.Player.SkipNext();
                        break;
                    case "back":
                        await Spotify.Client.Player.SkipPrevious();
                        break;
                    default:
                        Console.WriteLine($"Unknown action '{action}'...");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION in Player command: {ex.Message}");
                await RespondAsync($"There was an error! {ex.Message}");
            }
            await RespondAsync(ackReplies[random.Next(ackReplies.Count - 1)]);
        }

        [SlashCommand("volume", "Set the volume level (0-100).")]
        public async Task Volume(int level)
        {
            try
            {
                int cleanVolume = level < 0 ? 0 : Math.Min(100, level);
                await Spotify.Client.Player.SetVolume(new SpotifyAPI.Web.PlayerVolumeRequest(cleanVolume));
                await RespondAsync($"Set volume to: {cleanVolume}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION in Volume command: {ex.Message}");
                await RespondAsync($"Couldn't set volume: {ex.Message}");
            }
        }

        // TODO TODO: Test this method! Just queue and skip repeatedly. Might need more logging.
        [SlashCommand("queue-add", "jio")]
        public async Task QueueTrack(
            [Summary(name: "Keyword", description: "Brock will add the first track matching this search term.")] string query)
        {
            FullTrack track = (await Spotify.QueryTracksByName(query))[0];
            await Spotify.Client.Player.AddToQueue(new PlayerAddToQueueRequest(track.Uri));
            Console.WriteLine($"Queued {track.Name} by {track.Artists} thanks to {Context.User.Username}.");
            await RespondAsync($"Queued {track.Name} by {track.Artists[0].Name}.");
        }

        [SlashCommand("search", "Searches spotify for tracks matching the search phrase.")]
        public async Task Search(string query)
        {
            List<FullTrack> tracks = await Spotify.QueryTracksByName(query);
            Console.WriteLine($"Search(\"{query}\"): got {tracks.Count} results.");

            foreach (FullTrack track in tracks)
            {
                Console.WriteLine(track);
                Console.WriteLine($"(((manual))) {track.Name} -- {track.Artists[0]} --- href={track.Href}, id={track.Id}, uri={track.Uri}");
            }

            //var resultsMenuBuilder = new SelectMenuBuilder().WithPlaceholder("Select a result to queue it!");

            if (tracks.Count <= 0)
            {
                await RespondAsync($"No results found for {query}.", ephemeral: true);
                Console.WriteLine("HOW HOW HOW>???");
            }
            if (tracks.Count <= 0)
            {
                await RespondAsync("WTF???");
                Console.WriteLine("Okay this is BAD AND WEIRD");
            }

            List<SelectMenuOptionBuilder> options = new List<SelectMenuOptionBuilder>();
            foreach (FullTrack track in tracks)
            {
                options.Add(new SelectMenuOptionBuilder($"{TrackToString(track)}", track.Uri));
            }
            var resultsMenuBuilder = new SelectMenuBuilder().WithCustomId("menu1").WithPlaceholder("Select a result to queue it!").WithOptions(options);

            await RespondAsync(
                "Search Results", 
                components: new ComponentBuilder().WithSelectMenu(resultsMenuBuilder).Build(), 
                ephemeral: true);
        }

        private string TrackToString(FullTrack track)
        {
            return $"'{track.Name}' -- {String.Join(", ", track.Artists.Select(artist => artist.Name))}";
        }
    }
}
