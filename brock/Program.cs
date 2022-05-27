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
using Microsoft.Extensions.Hosting;
using Discord.Interactions;
using brock.Services;
using SpotifyAPI.Web;
using brock.Handlers;
using Microsoft.Extensions.Configuration;

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
        SpotifyService _spotify;

        public async Task MainAsync(string[] args)
        {
            Console.WriteLine($"BaseDirectory: {AppContext.BaseDirectory}, TargetFrameworkName: {AppContext.TargetFrameworkName}");
            var token = args[0];
            var spotifyToken = args[1];
            var spotifyClientID = args[2];
            var spotifyClientSecret = args[3];

            using (var services = ConfigureServices())
            {
                //_client = new DiscordSocketClient();
                _client = services.GetRequiredService<DiscordSocketClient>();
                _commands = services.GetRequiredService<InteractionService>();
                _spotify = services.GetRequiredService<SpotifyService>();
                
                _client.Log += Log;

                Console.WriteLine("Token: " + token);
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                services.GetRequiredService<ConfigService>().Initialize();
                await services.GetRequiredService<InteractionHandler>().InitializeAsync();
                services.GetRequiredService<SelectMenuHandlers>().Initialize();
                await _spotify.InitializeAsync(spotifyClientID, spotifyClientSecret);

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
                    Console.WriteLine($"Registered {cmd.Name}");
                }
                Console.WriteLine("FINISHED PRINTING COMMANDS");
            }
            catch (Exception ex) { 
                Console.WriteLine($"EXCEPTION registering commands: {ex.Message}"); 
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.HelpLink);
            }
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<ConfigService>()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton<SelectMenuHandlers>()
                .AddSingleton<SpotifyService>()
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
    }
}
