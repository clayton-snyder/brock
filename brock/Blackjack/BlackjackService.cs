using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static brock.Blackjack.BlackjackGame;

namespace brock.Blackjack
{
    /// <summary>
    /// Singleton that tracks existing Blackjack games, credits/debits based on game results, and (eventually)
    /// manages auditing/storage of results, credits, etc. (through interfacing with DB class?).
    /// DOES NOT process game logic.
    /// </summary>
    public class BlackjackService
    {
        Dictionary<string, BlackjackGame> ActiveGames;
        private const string LP = "[BLACKJACK - BlackjackService]";  // Log prefix

        public void Initialize()
        {
            this.ActiveGames = new Dictionary<string, BlackjackGame>();
            Console.WriteLine($"{LP} BlackjackService initialized.");
        }

        public bool StartGameForUser(SocketUser user, float wager)
        {
            Console.WriteLine($"{LP} Attempting to create new game for {user.Username} with wager {wager}.");
            if (ActiveGames.ContainsKey(user.Username))
            {
                Console.WriteLine($"{LP} Failed to start game for {user.Username} as a game already exists.");
                return false;
            }
            ActiveGames[user.Username] = new BlackjackGame(wager);
            Console.WriteLine($"{LP} New game successfully created for {user.Username} with wager {wager}.");
            return true;
        }

        public BlackjackGame GetUserCurrentGame(string username)
        {
            BlackjackGame currentGame;
            ActiveGames.TryGetValue(username, out currentGame);
            return currentGame;
        }

        public float ProcessFinishedGame(SocketUser user)
        {
            // Debit/credit winnings
            // Update player record
            // Clear game from dict
            BlackjackGame game = this.ActiveGames[user.Username];
            this.ActiveGames.Remove(user.Username);

            if (game == null)
            {
                Console.WriteLine($"{LP} ProcessFinishedGame didn't find a game for {user.Username}.");
                return 0.0f;
            }

            if (game.State == GameState.PlayerWon || game.State == GameState.DealerBust) return game.Wager;
            if (game.State == GameState.DealerWon || game.State == GameState.PlayerBust) return -game.Wager;
            if (game.State == GameState.PlayerWonNatural) return game.Wager * 1.5f;

            return 0.0f;
        }

        public bool ClearUserGame(string username)
        {
            Console.WriteLine($"{LP} Clearing game for {username}.");
            return ActiveGames.Remove(username);
        }
    }
}
