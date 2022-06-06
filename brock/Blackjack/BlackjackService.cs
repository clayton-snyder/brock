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

        public BlackjackGame GetUserCurrentGame(SocketUser user)
        {
            BlackjackGame currentGame;
            ActiveGames.TryGetValue(user.Username, out currentGame);
            return currentGame;
        }

        public async Task ProcessFinishedGame()
        {
            // Debit/credit winnings
            // Update player record
            // Clear game from dict
            throw new NotImplementedException();
        }
    }
}
