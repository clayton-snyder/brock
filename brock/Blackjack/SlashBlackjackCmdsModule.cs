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

        // TODO: Need big refactors here
        [SlashCommand("bet", "Start a new game with the chosen wager.")]
        public async Task bet([MinValue(1)] uint wager)
        {
            BlackjackGame currentGame = BlackjackService.GetUserCurrentGame(Context.User);
            if (currentGame != null)
            {
                await RespondAsync("Finish your current game first.");
                return;
            }

            //TODO: Check if user has sufficient funds for the wager. Or do that in service?

            bool created = BlackjackService.StartGameForUser(Context.User, wager);
            
            if (!created)
            {
                await RespondAsync($"Could not create game for unknown reason.");
                return;
            }

            currentGame = BlackjackService.GetUserCurrentGame(Context.User);

            if (currentGame == null)
            {
                await RespondAsync($"Supposedly the game was created but getting it returned null. Not great.");
            }

            // Game should be in Start state here
            currentGame.Tick();
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
            string playerHandString = String.Join(", ", currentGame.PlayerHand.Select(c => c.ToChatString()));
            ushort playerScore = currentGame.BestScore(currentGame.PlayerHand);
            
            switch (currentGame.State)
            {
                case GameState.PlayerChoose:
                    await RespondAsync($"You drew {currentGame.PlayerHand.Last().ToChatString()}.\n\n " +
                        $"Your hand: {playerHandString}");
                    break;
                case GameState.PlayerBust:
                    await BlackjackService.ProcessFinishedGame(currentGame);
                    await RespondAsync($"You drew {currentGame.PlayerHand.Last().ToChatString()}\n\n" +
                        $"Busted with a score of {playerScore}. Final hand: {playerHandString}.\n" +
                        $"Lost {currentGame.Wager} credits. Thank you so much for a-playing my game!");
                    break;
                case GameState.DealerDraw:
                    string response = $"You drew {currentGame.PlayerHand.Last().ToChatString()}.\n\n" +
                        $"Hand: {playerHandString}. That's a perfect score of 21!\n";
                    while (currentGame.State == GameState.DealerDraw)
                    {
                        currentGame.Tick();
                        response += $"Dealer drew {currentGame.DealerHand.Last().ToChatString()}\n";
                    }

                    string dealerHandString = String.Join(", ", currentGame.DealerHand.Select(c => c.ToChatString()));
                    ushort dealerScore = currentGame.BestScore(currentGame.DealerHand);
                    switch (currentGame.State)
                    {
                        case GameState.DealerBust:
                            response += $"Dealer busted with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"Won {currentGame.Wager} credits.";
                            break;
                        case GameState.PlayerWon:
                            response += $"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"You won with a score of {playerScore}.\n" +
                                $"Won {currentGame.Wager} credits.";
                            break;
                        case GameState.DealerWon:
                            response += $"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"You lost with a score of {playerScore}.\n" +
                                $"Lost {currentGame.Wager} credits. Thank you so much for a-playing my game!";
                            break;
                        case GameState.Push:
                            response += $"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"It's a push.\n" +
                                $"Your bet of {currentGame.Wager} credits is returned.";
                            break;
                        default:
                            response += $"BIG error. Not looking great. Unexpected GameState ({currentGame.State}) after " +
                                $"finishing dealer draws. Should be DealerBust({GameState.DealerBust}), PlayerWon(" +
                                $"{GameState.PlayerWon}), DealerWon({GameState.DealerWon}), or Push({GameState.Push}.";
                            break;
                    }

                    await BlackjackService.ProcessFinishedGame(currentGame);
                    await RespondAsync(response);
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
