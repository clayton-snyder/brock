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
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("++InteractionHandler.InitializeAsync()");
            // This is adding the "modules" we create (that inherit from InteractionModuleBase<T>) to actually
            // perform command logic. Who knows wtf an Assembly is.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;

            _commands.SlashCommandExecuted += SlashCommandExecuted;
            

        }

        private Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext ctx, IResult result)
        {
            Console.WriteLine($"RESULT of SlashCommand {info.Name}/{info.MethodName}/{info.Module.Name}:" +
                $"Success? {result.IsSuccess} - {result.Error} {result.ErrorReason}");
            return Task.CompletedTask;
        }
        
        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                Console.WriteLine($"INTERACTION CREATED, in HandleInteraction(). Type: {interaction.Type}");
                IInteractionContext ctx = new SocketInteractionContext(_client, interaction);
                Console.WriteLine($"Calling ExecuteCommandAsync from InteractionHandler.HandleInteraction...");
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in InteractionHandler.HandleInteraction: {ex.Message}");
                Console.WriteLine($"Now gonna delete this: '{await interaction.GetOriginalResponseAsync()}");

                if (interaction.Type == InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}
