using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace brock.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        public CommandHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("\tCommandHandler.InitializeAsync()");
            // This is adding the "modules" we create (that inherit from InteractionModuleBase<T>) to actually
            // perform command logic. Who knows wtf an Assembly is.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;

            _commands.SlashCommandExecuted += SlashCommandExecuted;
            //_commands.ContextCommandExecuted += ContextCommandExecuted;
            //_commands.ComponentCommandExecuted += ComponentCommandExecuted;

        }

        private Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext ctx, IResult result)
        {
            Console.WriteLine("Here I am in CommandHandler.SlashCommandExecuted!");
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        Console.WriteLine($"UnmetPrecondition: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.UnknownCommand:
                        Console.WriteLine($"UnknownCommand: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.BadArgs:
                        Console.WriteLine($"BadArgs: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.Exception:
                        Console.WriteLine($"Exception: {result.ErrorReason}");
                        break;
                    case InteractionCommandError.Unsuccessful:
                        Console.WriteLine($"Unsuccessful: {result.ErrorReason}");
                        break;
                    default:
                        break;
                }
            } else
            {
                Console.WriteLine("Command successful?");
            }
            return Task.CompletedTask;
        }
        
        private async Task HandleInteraction(SocketInteraction interaction)
        {
            Console.WriteLine($"INTERACTION CREATED, in HandleInteraction(). Data: {interaction.Data}");
            IInteractionContext ctx = new SocketInteractionContext(_client, interaction);
            await _commands.ExecuteCommandAsync(ctx, _services);
        }
    }
}
