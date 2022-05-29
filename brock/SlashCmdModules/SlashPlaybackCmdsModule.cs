using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace brock.Services
{
    public class SlashPlaybackCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        public SpotifyService Spotify { get; set; }
        public DiscordSocketClient client { get; set; }
        private readonly ConfigService _config;

        private readonly List<string> ackReplies = new List<string> { "OK!", "Got it!", "Yes!", "No problem!", "Done!" };
        private static readonly Random random = new Random();

        public SlashPlaybackCmdsModule(ConfigService config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            _config = config;
        }

        [SlashCommand("player", "Control the music playback.")]
        public async Task Player(
            [Choice("play", "play"),
            Choice("pause", "pause"), 
            Choice("skip", "skip"),
            Choice("back", "back"),
            Choice("show", "show")] string action)
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
                    case "show":
                        CurrentlyPlayingContext playbackContext = await Spotify.Client.Player.GetCurrentPlayback();
                        FullTrack track = (FullTrack) playbackContext.Item;
                        TimeSpan progress = TimeSpan.FromMilliseconds(playbackContext.ProgressMs);
                        TimeSpan duration = TimeSpan.FromMilliseconds(track.DurationMs);

                        var summaryEmbed = new EmbedBuilder { Title = $"Currently Playing" };
                        summaryEmbed.AddField("Track", track.Name);
                        summaryEmbed.AddField("Artist", $"{String.Join(", ", track.Artists.Select(a => a.Name))}");
                        summaryEmbed.AddField("Album", $"{track.Album.Name}");
                        summaryEmbed.AddField("Progress", $"{TimeSpanString(progress)}  /  {TimeSpanString(duration)}");
                        summaryEmbed.WithCurrentTimestamp().WithColor(Color.Blue);
                        await RespondAsync(embed: summaryEmbed.Build());
                        return;
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

        private string TimeSpanString(TimeSpan t)
        {
            string padDays = $"{t.Days}".PadLeft(2, '0');
            string padHours = $"{t.Hours}".PadLeft(2, '0');
            string padMins = $"{t.Minutes}".PadLeft(2, '0');
            string padSecs = $"{t.Seconds}".PadLeft(2, '0');
            if (t.Days > 0) return $"{padDays}:{padHours}:{padMins}:{padSecs}";
            if (t.Hours > 0) return $"{padHours}:{padMins}:{padSecs}";
            return $"{padMins}:{padSecs}";
        }

        [SlashCommand("volume", "Set the volume level (0-100). This will affect volume for everyone.")]
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
        [SlashCommand("qq", "Quick queue - add the first track matching a search phrase to the queue.")]
        public async Task QueueTrack(
            [Summary(name: "query", description: "Brock will add the first track matching this search term.")] string query)
        {
            FullTrack track = (await Spotify.QueryTracksByName(query))[0];
            await Spotify.Client.Player.AddToQueue(new PlayerAddToQueueRequest(track.Uri));
            Console.WriteLine($"Queued {track.Name} by {track.Artists} thanks to {Context.User.Username}.");
            await RespondAsync($"{Context.User.Username} queued *{track.Name}* by {track.Artists[0].Name}.");
        }

        [SlashCommand("search", "Search Spotify for tracks matching a phrase. You can select a result to queue it.")]
        public async Task Search(string query, [MinValue(1)] [MaxValue(20)] int maxResults = 5)
        {
            List<FullTrack> tracks = await Spotify.QueryTracksByName(query, maxResults);
            Console.WriteLine($"Search(\"{query}\"): got {tracks.Count} results.");

            if (tracks.Count <= 0)
            {
                await RespondAsync($"No results found for {query}.", ephemeral: true);
                return;
            }

            List<SelectMenuOptionBuilder> options = new List<SelectMenuOptionBuilder>();
            foreach (FullTrack track in tracks)
            {
                // Strangely, you can only pass a string as a MenuOption value, so we have to delimit that value so that
                // the menu selection handler can parse URI, title, and artists
                Dictionary<string, string> trackStrings = TrackStrings(track, 100);
                options.Add(new SelectMenuOptionBuilder($"{trackStrings["name"]}", track.Id, $"{trackStrings["artists"]}"));
            }
            var resultsMenuBuilder = new SelectMenuBuilder().WithCustomId("search-results").WithPlaceholder("Select a result to queue it!").WithOptions(options);

            await RespondAsync(
                "Search Results", 
                components: new ComponentBuilder().WithSelectMenu(resultsMenuBuilder).Build(), 
                ephemeral: true);
        }

        /// <summary>
        /// Don't pass less than 4 as maxLength or else it will be ignored.
        /// This is because you need at least 3 characters for "...", plus one character to be meaningful.
        /// </summary>
        /// <param name="track"></param>
        /// <param name="maxLength"></param>
        /// <returns>One string with track name and artists.</returns>
        private string TrackToString(FullTrack track, int maxLength = 0)
        {
            string str = $"'{track.Name}' -- {String.Join(", ", track.Artists.Select(artist => artist.Name))}";
            if (str.Length >= maxLength && maxLength > 3)
            {
                str = str.Substring(0, maxLength - 3) + "...";
            }
            return str;
        }

        /// <summary>
        /// Returns a Dictionary with strings that are parsed from the track and abbreviated according
        /// to maxLength. Values less than 4 of maxLength will be ignored. Note that if a string exceeds
        /// maxLength, it is truncated and the last three characters replaced with "...".
        /// NOTE: maxLength does NOT apply to trackUri, otherwise it would not function as a URI :)
        /// </summary>
        /// <param name="track"></param>
        /// <param name="maxLength"></param>
        /// <returns>Dictionary<string, string> with keys "name", "artists", and "uri".</returns>
        private Dictionary<string, string> TrackStrings(FullTrack track, int maxLength = 0)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string name = track.Name;
            string artists = String.Join(", ", track.Artists.Select(artist => artist.Name));

            if (name.Length >= maxLength && maxLength > 3) name = name.Substring(0, maxLength - 3) + "...";
            if (artists.Length >= maxLength && maxLength > 3) artists = artists.Substring(0, maxLength - 3) + "...";

            result.Add("name", name);
            result.Add("artists", artists);
            result.Add("uri", track.Uri);
            return result;
        }
    }
}
