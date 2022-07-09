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
        private const string LP = "[BLACKJACK - SlashBlackjackCmdsModule]";

        public BlackjackService BlackjackService { get; set; }

        // TODO: Need big refactors here
        [SlashCommand("bet", "Start a new game with the chosen wager.")]
        public async Task Bet([MinValue(1)] uint wager)
        {
            BlackjackGame currentGame = BlackjackService.GetUserCurrentGame(Context.User);
            if (currentGame != null)
            {
                await RespondAsync("Finish your current game first.");
                return;
            }

            //TODO: Check if user has sufficient funds for the wager. Or do that in service?

            
            if (!BlackjackService.StartGameForUser(Context.User, wager))
            {
                await RespondAsync($"Could not create game for unknown reason.");
                return;
            }

            currentGame = BlackjackService.GetUserCurrentGame(Context.User);

            if (currentGame == null)
            {
                await RespondAsync($"Supposedly the game was created but getting it returned null. Not great.");
            }

            Console.WriteLine($"{LP} Fetched created game, now calling first 'Tick()'.");
            currentGame.Tick();
            Console.WriteLine($"{LP} Finished the tick.");

            string response = $"Your hand: {String.Join(", ", currentGame.PlayerHand.Select(c => c.ToChatString()))}\n";
            if (currentGame.State == GameState.PlayerWonNatural)
            {
                response += "You cheated and got a blackjack.\n";
                float credits = BlackjackService.ProcessFinishedGame(Context.User);
                response += $"Won {credits} credits.";
            } 
            else if (currentGame.State == GameState.Push)
            {
                response += $"Dealer hand: {currentGame.DealerHand.Select(c => c.ToChatString())}\n";
                response += $"It's a push. Your wager of {currentGame.Wager} is returned.";
                BlackjackService.ProcessFinishedGame(Context.User);
            }

            Console.WriteLine($"{LP} Now responding with: \"{response}\"");
            await RespondAsync(response);
        }

        [SlashCommand("hit", "Take another card.")]
        public async Task Hit()
        {
            BlackjackGame currentGame = BlackjackService.GetUserCurrentGame(Context.User);
            if (currentGame == null)
            {
                await RespondAsync("Couldn't find an existing game. Start a new game by placing a bet.");
                return;
            }

            try
            {
                currentGame.Tick(PlayerChoice.Hit);
            }
            catch (Exception e)
            {
                await RespondAsync(e.Message);
                return;
            }


            string playerHandString = String.Join(", ", currentGame.PlayerHand.Select(c => c.ToChatString()));
            ushort playerScore = currentGame.BestScore(currentGame.PlayerHand);
            
            switch (currentGame.State)
            {
                case GameState.PlayerChoose:
                    await RespondAsync($"You drew {currentGame.PlayerHand.Last().ToChatString()}.\n\n " +
                        $"Your hand: {playerHandString}");
                    break;
                case GameState.PlayerBust:
                    BlackjackService.ProcessFinishedGame(Context.User);
                    await RespondAsync($"You drew {currentGame.PlayerHand.Last().ToChatString()}\n\n" +
                        $"Busted with a score of {playerScore}. Final hand: {playerHandString}.\n" +
                        $"Lost {currentGame.Wager} credits.");
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
                                $"Lost {currentGame.Wager} credits.";
                            break;
                        case GameState.Push:
                            response += $"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"It's a push.\n" +
                                $"Your wager of {currentGame.Wager} credits is returned.";
                            break;
                        default:
                            response += $"BIG error. Not looking great. Unexpected GameState ({currentGame.State}) after " +
                                $"finishing dealer draws. Should be DealerBust({GameState.DealerBust}), PlayerWon(" +
                                $"{GameState.PlayerWon}), DealerWon({GameState.DealerWon}), or Push({GameState.Push}.";
                            break;
                    }

                    BlackjackService.ProcessFinishedGame(Context.User);
                    await RespondAsync(response);
                    break;
                    //TODO: rest of post-tick states (i think just bust?)
            }
        }

        [SlashCommand("stand", "Keep your current hand.")]
        public async Task Stand()
        {
            BlackjackGame currentGame = BlackjackService.GetUserCurrentGame(Context.User);
            if (currentGame == null)
            {
                await RespondAsync("Couldn't find an existing game. Start a new game by placing a bet.");
                return;
            }

            try
            {
                currentGame.Tick(PlayerChoice.Stand);
            }
            catch (Exception e)
            {
                await RespondAsync(e.Message);
                return;
            }

            if (currentGame.State != GameState.DealerDraw)
            {
                Console.WriteLine($"{LP} Invalid game state after Tick on PlayerChoice.Stand: {currentGame.State}");
                await RespondAsync("There was a problem. Game aborted.");
                BlackjackService.ClearUserGame(Context.User);
            }

            while (currentGame.State == GameState.DealerDraw)
            {
                try
                {
                    currentGame.Tick();
                }
                catch (Exception e)
                {
                    await RespondAsync(e.Message);
                    return;
                }
            }

            //TODO: Respond with result of game.
        }

        [SlashCommand("show", "Show your current game.")]
        public async Task Show()
        {
            BlackjackGame currentGame = BlackjackService.GetUserCurrentGame(Context.User);
            if (currentGame == null)
            {
                await RespondAsync("Couldn't find an existing game. Start a new game by placing a bet.");
                return;
            }

            throw new NotImplementedException();
        }
    }
}
