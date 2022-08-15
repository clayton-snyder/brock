using brock.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static brock.Blackjack.BlackjackGame;
using static brock.Blackjack.BlackjackService;

namespace brock.Blackjack
{

    [Group("blackjack", "We all love a game of BLACKJACK. Enjoy a game of Blackjack.")]
    public class SlashBlackjackCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private const string LP = "[BLACKJACK - SlashBlackjackCmdsModule]";
        private readonly DiscordSocketClient _client;
        private readonly ConfigService _config;
        public SlashBlackjackCmdsModule(DiscordSocketClient socketClient, ConfigService config)
        {
            _client = socketClient;
            _config = config;
            _client.ButtonExecuted += ButtonExecuted;
            _client.ModalSubmitted += ModalSubmitted;
        }
        public BlackjackService BlackjackService { get; set; }

        private async Task ButtonExecuted(SocketMessageComponent arg)
        {
            Console.WriteLine($"{LP} ButtonExecuted - {arg.Type} - {arg.Data.Type} - {arg.CreatedAt} - RESPONDED?{arg.HasResponded}");
            if (arg.HasResponded)
            {
                Console.WriteLine($"{LP} Ignoring ButtonExecuted event due to already responded.");
                return;
            }

            (string Response, ButtonGroup IncludedButtons) result;
            switch (arg.Data.CustomId)
            {
                case "blackjack-hit":
                    result = BlackjackService.Hit(arg.User.Username);
                    await arg.RespondAsync(result.Response, components: GetButtonComponent(result.IncludedButtons));
                    break;
                case "blackjack-stand":
                    result = BlackjackService.Stand(arg.User.Username);
                    await arg.RespondAsync(result.Response, components: GetButtonComponent(result.IncludedButtons));
                    break;
                case "blackjack-play-again":
                    await arg.RespondWithModalAsync(NewBetModal());
                    break;
            }
        }

        private async Task ModalSubmitted(SocketModal arg)
        {
            switch (arg.Data.CustomId)
            {
                case "blackjack-new-bet":
                    uint wager;
                    try
                    {
                        wager = UInt32.Parse(arg.Data.Components.ToList().First(x => x.CustomId == "wager").Value);
                        if (wager > _config.Get<uint>("BlackjackMaxWager"))
                        {
                            throw new ArgumentOutOfRangeException($"Wager must be positive and under {_config.Get<uint>("BlackjackMaxWager")}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{LP} Invalid new wager from modal: {e.Message}");
                        await arg.RespondAsync(
                            $"Idiot, enter a positive integer under {_config.Get<uint>("BlackjackMaxWager")}.",
                            components: GetButtonComponent(ButtonGroup.PlayAgain)
                        );
                        return;
                    }
                    (string Response, ButtonGroup IncludedButtons) result = BlackjackService.Bet(arg.User.Username, wager);
                    await arg.RespondAsync(result.Response, components: GetButtonComponent(result.IncludedButtons));
                    return;                    
            }
        }

        [SlashCommand("bet", "Start a new game with the chosen wager.")]
        public async Task Bet([MinValue(1)] uint wager)
        {
            var result = BlackjackService.Bet(Context.User.Username, wager);
            await RespondAsync(result.Response, components: GetButtonComponent(result.IncludedButtons));
        }

        [SlashCommand("hit", "Take another card.")]
        public async Task HitCommand()
        {
            (string Response, ButtonGroup IncludedButtons) result = BlackjackService.Hit(Context.User.Username);
            await RespondAsync(result.Response, components: GetButtonComponent(result.IncludedButtons));
        }

        [SlashCommand("stand", "Keep your current hand.")]
        public async Task StandCommand()
        {
            (string Response, ButtonGroup IncludedButtons) result = BlackjackService.Stand(Context.User.Username);
            await RespondAsync(result.Response, components: GetButtonComponent(result.IncludedButtons));
        }

        [SlashCommand("show", "Show your current game.")]
        public async Task ShowCommand()
        {
            BlackjackGame currentGame = BlackjackService.GetUserCurrentGame(Context.User.Username);
            if (currentGame == null)
            {
                await RespondAsync("Couldn't find an existing game. Start a new game by placing a bet.");
                return;
            }

            await RespondAsync(currentGame.ToChatString(), components: currentGame.State == GameState.PlayerChoose ? GetButtonComponent(ButtonGroup.HitStand) : null);
        }

        [SlashCommand("cleargame", "(admin) Clear game for specified user.")]
        public async Task ClearGame(String username)
        {
            if (!Context.User.Username.ToLower().Equals("clant"))
            {
                await RespondAsync($"User {Context.User.Username} is not allowed to do this.");
                return;
            }

            if (BlackjackService.GetUserCurrentGame(username) == null)
            {
                await RespondAsync($"Didn't find a game for '{username}'.");
                return;
            }

            if (BlackjackService.ClearUserGame(username))
            {
                await RespondAsync($"Cleared {username}'s game.");
            }
            else
            {
                await RespondAsync($"ClearUserGame() returned false despite GetUserCurrentGame not returning null?");
            }
        }

        private Modal NewBetModal()
        {
            return new ModalBuilder()
                .WithTitle("New Bet")
                .WithCustomId("blackjack-new-bet")
                .AddTextInput("Wager:", "wager", TextInputStyle.Short)
                .Build();
        }

        private MessageComponent GetButtonComponent(ButtonGroup buttonGroup)
        {
            switch (buttonGroup)
            {
                case ButtonGroup.None:
                    return null;
                case ButtonGroup.HitStand:
                    return new ComponentBuilder()
                        .WithButton("Hit", "blackjack-hit", ButtonStyle.Success)
                        .WithButton("Stand", "blackjack-stand", ButtonStyle.Danger)
                        .Build();
                case ButtonGroup.PlayAgain:
                    return new ComponentBuilder()
                        .WithButton("Play Again", "blackjack-play-again", ButtonStyle.Primary)
                        .Build();
            }

            Console.WriteLine($"{LP} Error! GetButtonComponent() fell out of switch! Unknown ButtonGroup '{buttonGroup}'?");
            return null;
        }
    }
}
