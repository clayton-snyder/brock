using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Services
{
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        // NOTE: Two ways of dependency injection below. Either give the dependency reference a public
        // getter/setter or inject it in the constructor. (Different naming convention due to priv var?)
        //public InteractionService Commands { get; set; }
        //private readonly CommandHandler _handler;

/*        public SlashCommands(CommandHandler handler)
        {
            Console.WriteLine("SlashCommands constructor");
            _handler = handler;
        }
*/
        [SlashCommand("bring", "Play bring game!", runMode:RunMode.Async)]
        public async Task Bing([Choice("Ring", "ring"), Choice("Rong", "rong"), Choice("Gone", "goible")] string yourChoice)
        {
            await RespondAsync($"You have chosen {yourChoice}.");
        }
    }
}
