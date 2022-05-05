using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Services
{
    internal class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        // NOTE: Two ways of dependency injection below. Either give the dependency reference a public
        // getter/setter or inject it in the constructor. (Different naming convention due to priv var?)
        public InteractionService Commands { get; set; }
        private CommandHandler _handler;

        public SlashCommands(CommandHandler handler)
        {
            _handler = handler;
        }

        [SlashCommand("bing", "Play bing game!")]
        public async Task Bing(string input)
        {
            await RespondAsync($"My response to {input} is bong {input}.");
        }
    }
}
