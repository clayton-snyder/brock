using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord.Audio;
using Microsoft.Extensions.DependencyInjection;
using Discord.Interactions;
using brock.Services;
using System.Collections.Generic;

// TODO: https://makolyte.com/csharp-parsing-commands-and-arguments-in-a-console-app/#Using_CommandLineParser_to_parse_commands_and_arguments
// TODO: Add slash commands! Self-documenting!
// TODO: Can the bin stuff be automated?
namespace brock
{
    internal class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync(args);

        DiscordSocketClient _client;
        InteractionService _commands;

        public async Task MainAsync(string[] args)
        {
            var token = args[0];

            using (var services = ConfigureServices())
            {
                //_client = new DiscordSocketClient();
                _client = services.GetRequiredService<DiscordSocketClient>();
                _commands = services.GetRequiredService<InteractionService>();

                _client.Log += Log;

                Console.WriteLine("Token: " + token);
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                Console.WriteLine("SHOULD BE ABOUT TO SEE CommandHandler do InitializeAsync()...");
                await services.GetRequiredService<CommandHandler>().InitializeAsync();
                Console.WriteLine("Did it do it?");

                //_client.MessageReceived += HandleMessage;
                _client.Ready += ReadyAsync;

                // Block task until program is closed
                await Task.Delay(-1);
            }
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine("Here are the slash commands registered in InteractionService");
            foreach (var slashCmd in _commands.SlashCommands)
            {
                Console.WriteLine(slashCmd);
            }
            try
            {
                IReadOnlyCollection<Discord.Rest.RestGuildCommand> cmds = await _commands.RegisterCommandsToGuildAsync(252302649884409859);
                foreach(var cmd in cmds)
                {
                    Console.WriteLine(cmd.Name);
                }
                Console.WriteLine("FINISHED PRINTING COMMANDS");
            }
            catch (Exception ex) { Console.WriteLine($"EXCEPTION registering commands: {ex.Message}"); }
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        /*private Task HandleMessage(SocketMessage msg)
        {
            Console.WriteLine(String.Format("{0} said {1}", msg.Author, msg.Content));
            switch (msg.Content.ToLower())
            {
                case "ping":
                    return msg.Channel.SendMessageAsync("pong");
                case "voice":
                    JoinChannel((msg.Author as IGuildUser).VoiceChannel);
                    Console.WriteLine("AFTER JoinChannel");
                    return Task.CompletedTask;
                default:
                    return Task.CompletedTask;
            }
        }*/

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
    }
}
