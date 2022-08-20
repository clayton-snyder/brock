using brock.Services;
using brock.Handlers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using brock.Blackjack;

namespace brock
{
    /*
     * NEXT IDES
     * -Keep updating Currently Playing embed? At least click-to-refresh message context option?
     * -Context menu options for pause, skip, play, leave voice
     * */
    internal class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync(args);

        ConfigService _config;
        DiscordSocketClient _client;
        InteractionService _commands;
        SpotifyService _spotify;

        public async Task MainAsync(string[] args)
        {
            Console.WriteLine($"In the beginning, God created the heavens and the earth.\n");
            const string LP = "[MainAsync]";



            using (var services = ConfigureServices())
            {
                // Have to load ConfigService first since we get Discord token from it
                _config = services.GetRequiredService<ConfigService>();
                _config.Initialize();
                string discordToken = _config.Get<string>("DiscordToken");
                string spotifyClientID = _config.Get<string>("SpotifyClientID");
                string spotifyClientSecret = _config.Get<string>("SpotifyClientSecret");
                Console.WriteLine($"{LP} Loaded from config:\n" +
                    $"\tDiscordToken:{discordToken}, " +
                    $"\n\tSpotifyClientID:{spotifyClientID}, " +
                    $"\n\tSpotifyClientSecret:{spotifyClientSecret}");

                _client = services.GetRequiredService<DiscordSocketClient>();
                _commands = services.GetRequiredService<InteractionService>();
                _spotify = services.GetRequiredService<SpotifyService>();

                services.GetRequiredService<ComponentHandlers>().Initialize();
                await services.GetRequiredService<InteractionHandler>().InitializeAsync();
                await _spotify.InitializeAsync();
                services.GetRequiredService<BlackjackService>().Initialize();
                
                services.GetRequiredService<DatabaseService>().Initialize();

                _client.Ready += ReadyAsync;
                Console.WriteLine($"{LP} Attemtping LoginAsync().");
                await _client.LoginAsync(TokenType.Bot, discordToken);
                Console.WriteLine($"{LP} Attempting StartAsync().");
                await _client.StartAsync();

                // ScienceService must be Initialized AFTER client login so it has access to guilds/channels
                services.GetRequiredService<ScienceService>().Initialize();

                // Block task until program is closed
                Console.WriteLine($"{LP} Init steps finished, now blocking.");
                await Task.Delay(-1);
            }
        }

        private async Task ReadyAsync()
        {
            try
            {
#if DEBUG
                Console.WriteLine($"DEBUG - Registering to test guild {_config.Get<ulong>("TestGuildID")}");
                IReadOnlyCollection<Discord.Rest.RestGuildCommand> cmds = await _commands.RegisterCommandsToGuildAsync(_config.Get<ulong>("TestGuildID"));
                Console.WriteLine($"Registered: {String.Join(", ", cmds.Select(cmd => cmd.Name))} to TEST GUILD");
#else
                Console.WriteLine("Registering globally!");
                IReadOnlyCollection<Discord.Rest.RestGlobalCommand> cmds = await _commands.RegisterCommandsGloballyAsync();
                Console.WriteLine($"Registered: {String.Join(", ", cmds.Select(cmd => cmd.Name))} globally.");
#endif

                //foreach (SocketGuild guild in _client.Guilds)
                //{
                //    Console.Write($"**Registering commands for: {guild.Name} - {guild.Description} - {guild.Id}... ");
                //    IReadOnlyCollection<Discord.Rest.RestGuildCommand> cmds = await _commands.RegisterCommandsToGuildAsync(guild.Id);
                //    Console.WriteLine($"Registered: {String.Join(", ", cmds.Select(cmd => cmd.Name))}");
                //    Console.WriteLine($"From the command service: {String.Join(", ", _commands.SlashCommands.Select(cmd => cmd.Name))}");
                //}
                Console.WriteLine("**Finished registering commands.");
            }
            catch (Exception ex) { 
                Console.WriteLine($"\n**EXCEPTION registering commands: {ex.Message}"); 
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
                .AddSingleton<ComponentHandlers>()
                .AddSingleton<SpotifyService>()
                .AddSingleton<BlackjackService>()
                .AddSingleton<ScienceService>()
                .AddSingleton<DatabaseService>()
                .BuildServiceProvider();
        }
    }
}
