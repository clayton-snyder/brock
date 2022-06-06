using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static brock.Blackjack.BlackjackGame;

namespace brock.Blackjack
{
    [Group("blackjack", "We all love a game of BLACKJACK. Enjoy a game of Blackjack.")]
    public class SlashBlackjackCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        public BlackjackService BlackjackService { get; set; }

        [SlashCommand("bet", "Start a new game with the chosen bet.")]
        public async Task bet([MinValue(1)] uint amount)
        {
            throw new NotImplementedException();
        }

        [SlashCommand("hit", "Take another card.")]
        public async Task hit()
        {
            BlackjackGame currentGame = BlackjackService.GetUserCurrentGame(Context.User);
            if (currentGame == null)
            {
                await RespondAsync("Couldn't find an existing game. Start a new game by placing a bet.");
                return;
            }

            currentGame.Tick(PlayerChoice.Hit);
            
            switch (currentGame.State)
            {
                case GameState.PlayerChoose:
                    await RespondAsync($"You drew {currentGame.PlayerHand.Last()}. Your hand: ");
                    break;

                    //TODO: rest of post-tick states (i think just bust?)
            }
        }

        [SlashCommand("stand", "Keep your current hand.")]
        public async Task stand()
        {
            throw new NotImplementedException();
        }

        [SlashCommand("show", "Show your current game.")]
        public async Task show()
        {
            throw new NotImplementedException();
        }
        
        
    }
}
