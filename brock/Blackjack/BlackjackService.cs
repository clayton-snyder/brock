using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Initialize()
        {
            this.ActiveGames = new Dictionary<string, BlackjackGame>();
        }

        public bool StartGameForUser(SocketUser user, uint wager)
        {
            Console.WriteLine($"Attempting to create new game for {user.Username} with wager {wager}.");
            if (ActiveGames.ContainsKey(user.Username))
            {
                Console.WriteLine($"Failed to start game for {user.Username} as a game already exists.");
                return false;
            }
            ActiveGames[user.Username] = new BlackjackGame(wager);
            Console.WriteLine($"New game successfully created for {user.Username} with wager {wager}.");
            return true;
        }

        public BlackjackGame GetUserCurrentGame(SocketUser user)
        {
            BlackjackGame currentGame;
            ActiveGames.TryGetValue(user.Username, out currentGame);
            return currentGame;
        }

        public async Task ProcessFinishedGame(BlackjackGame game)
        {
            // Debit/credit winnings
            // Update player record
            // Clear game from dict
            throw new NotImplementedException();
        }
    }
}
