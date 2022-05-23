using brock.Services;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.SlashCmdModules
{
    public class SlashManageBotCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        public SpotifyService Spotify { get; set; }

        [SlashCommand("join", "Ask Brock to join the voice channel.", runMode: RunMode.Async)]
        public async Task JoinVoice()
        {
            var voiceChannel = (Context.User as IGuildUser).VoiceChannel;
            if (voiceChannel == null)
            {
                await RespondAsync("Join a voice channel first.");
                return;
            }

            IAudioClient audioClient = null;
            try
            {
                audioClient = await voiceChannel.ConnectAsync();
                RespondAsync("Joined?");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception joining voice: {ex.Message}\n{ex.StackTrace}");
                await RespondAsync($"Problem joining voice: {ex.Message}");
                return;
            }

            string micInput = "\"Line 1 (Virtual Audio Cable)\"";
            //string micInput = ""
            try
            {
                using (var ffmpeg = CreateStream($"audio={micInput}"))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Music))
                {
                    Console.WriteLine("Created process and streams, now trying to talk");
                    try { await output.CopyToAsync(discord); }
                    finally { await discord.FlushAsync(); }
                }
            }
            catch (Exception ex) { Console.WriteLine($"EXCEPTION: {ex.Message}"); }

            Console.WriteLine("Leaving now");
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                //Arguments = $"-hide_banner -loglevel panic -f dshow -i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                Arguments = $"-hide_banner -loglevel panic -f dshow -i {path} -f wav -ar 48k pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        [SlashCommand("leave", "Ask Brock to stop playing music and leave the voice channel.")]
        public async Task LeaveVoice()
        {
            await Spotify.Client.Player.PausePlayback();
            foreach (SocketVoiceChannel voiceChannel in Context.Guild.VoiceChannels)
            {
                await voiceChannel.DisconnectAsync();
            }
            Console.WriteLine("Disconnected from all voice channels.");
            await RespondAsync("Bye!");
        }
    }
}
        /*
 [Command("join", RunMode = Discord.Commands.RunMode.Async)]
 private async Task JoinChannel(IVoiceChannel channel = null)
 {
     Console.WriteLine("IN JoinChannel");
     channel = channel ?? throw new ArgumentNullException(nameof(channel));
     Console.WriteLine("Joined channel...");

     IAudioClient audioClient = null;
     try
     {
         audioClient = await channel.ConnectAsync();
     } catch (Exception ex)
     {
         Console.WriteLine($"EXCEPTION: {ex.Message}");
     }

     Console.WriteLine("Trying to create process and streams");
     // Can we use Opus stream instead of PCM? How do it sound?
     // Does `AudioApplication.Music` sound better than Mixed?
     try
     {
         using (var ffmpeg = CreateStream("audio=\"Stereo Mix (Realtek(R) Audio)\""))
         using (var output = ffmpeg.StandardOutput.BaseStream)
         using (var discord = audioClient.CreatePCMStream(AudioApplication.Music))
         {
             Console.WriteLine("Created process and streams, now trying to talk");
             try { await output.CopyToAsync(discord); }
             finally { await discord.FlushAsync(); }
         }
     } catch (Exception ex) { Console.WriteLine($"EXCEPTION: {ex.Message}"); }
 }

 private Process CreateStream(string path)
 {
     //return Process.Start("ffmpeg", "-f dshow -i audio=\"Stereo Mix (Realtek(R) Audio)\" D:\\Audio\\test_ffmpeg4.mp3");
     return Process.Start(new ProcessStartInfo {
         FileName = "ffmpeg",
         Arguments = $"-hide_banner -loglevel panic -f dshow -i {path} -ac 2 -f s16le -ar 48000 pipe:1",
         UseShellExecute = false,
         RedirectStandardOutput = true,
     });
 }*/
