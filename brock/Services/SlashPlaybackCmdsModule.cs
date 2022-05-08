using Discord.Interactions;
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
            await RespondAsync(ackReplies[new Random().Next(ackReplies.Count - 1)]);
        }

        [SlashCommand("volume", "Set the volume (0-100).")]
        public async Task Volume(int volume)
        {
            try
            {
                int cleanVolume = volume < 0 ? 0 : Math.Min(100, volume);
                await Spotify.Client.Player.SetVolume(new SpotifyAPI.Web.PlayerVolumeRequest(cleanVolume));
                await RespondAsync($"Set volume to: {cleanVolume}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION in Volume command: {ex.Message}");
                await RespondAsync($"Couldn't set volume: {ex.Message}");
            }
        }
    }
}
