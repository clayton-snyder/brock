using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//TODO: https://makolyte.com/csharp-parsing-commands-and-arguments-in-a-console-app/#Using_CommandLineParser_to_parse_commands_and_arguments
namespace brock
{
    internal class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync(args);

        DiscordSocketClient _client;

        public async Task MainAsync(string[] args)
        {
            var token = args[0];
            _client = new DiscordSocketClient();
            _client.Log += Log;

            Console.WriteLine("Token: " + token);
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block task until program is closed
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
