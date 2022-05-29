using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;

namespace brock.Services
{
    public class SlashDevCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        // NOTE: Two ways of dependency injection below. Either give the dependency reference a public
        // getter/setter or inject it in the constructor. (Different naming convention due to priv var?)
        public InteractionService Commands { get; set; }
        private readonly InteractionHandler _handler;

        public SlashDevCmdsModule(InteractionHandler handler)
        {
            _handler = handler;
        }

        [SlashCommand("bring", "Play bring game!", runMode:RunMode.Async)]
        public async Task Bing([Choice("Ring", "ring"), Choice("Rong", "rong"), Choice("Gone", "goible")] string yourChoice)
        {
            await RespondAsync($"You have chosen {yourChoice}.");
        }

        [SlashCommand("ppong", "Perform a ppong.")]
        public async Task Ppong(string input)
        {
            Console.WriteLine("In SlashCommands.Ppong, now trying to respond...");
            await RespondAsync($"Ping {input}");
            //return Task.CompletedTask;
        }

        [UserCommand("testUserContextCommand")]
        public async Task TestUserCtx(IUser user)
        {
            await RespondAsync($"User: {user.Discriminator}");
        }

        [MessageCommand("testMessageContextCommand")]
        public async Task TestMsgCtx(IMessage msg)
        {
            await RespondAsync($"Msg author: {msg.Author}");
        }
    }
}
