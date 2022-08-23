using brock.Services;
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
    /// DOES NOT process game logic. Just kidding it DOES.
    /// </summary>
    public class BlackjackService
    {
        Dictionary<string, BlackjackGame> ActiveGames;
        private const string LP = "[BLACKJACK - BlackjackService]";  // Log prefix
        private readonly ConfigService _config;
        private readonly DiscordSocketClient _client;

        public enum ButtonGroup
        {
            None,
            HitStand,
            PlayAgain
        }

        public BlackjackService(ConfigService config = null, DiscordSocketClient client = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException($"{LP} Constructor - config arg is null?");
            }
            if (client == null)
            {
                throw new ArgumentNullException($"{LP} Constructor - client arg is null?");
            }
            _config = config;
            _client = client;
        }

        public void Initialize()
        {
            this.ActiveGames = new Dictionary<string, BlackjackGame>();
            Console.WriteLine($"{LP} BlackjackService initialized.");
        }

        public bool StartGameForUser(string username, float wager)
        {
            Console.WriteLine($"{LP} Attempting to create new game for {username} with wager {wager}.");
            if (ActiveGames.ContainsKey(username))
            {
                Console.WriteLine($"{LP} Failed to start game for {username} as a game already exists.");
                return false;
            }
            ActiveGames[username] = new BlackjackGame(wager);
            Console.WriteLine($"{LP} New game successfully created for {username} with wager {wager}.");
            return true;
        }

        public BlackjackGame GetUserCurrentGame(string username)
        {
            BlackjackGame currentGame;
            ActiveGames.TryGetValue(username, out currentGame);
            return currentGame;
        }

        public float ProcessFinishedGame(string username)
        {
            // Debit/credit winnings
            // Update player record
            // Clear game from dict
            BlackjackGame game = this.ActiveGames[username];
            this.ActiveGames.Remove(username);

            if (game == null)
            {
                Console.WriteLine($"{LP} ProcessFinishedGame didn't find a game for {username}.");
                return 0.0f;
            }

            int recordsAdded = LogGameResultInDb(game, username, false);
            if (recordsAdded < 1)
            {
                Console.WriteLine($"{LP} recordsAdded < 1, discarding game");
                throw new Exception($"Unexpected number of rows affected from DB insert: {recordsAdded}. Game discarded.");
            }

            Console.WriteLine($"{LP} DB Log passed the Exception territory!!!");
            if (game.State == GameState.PlayerWon || game.State == GameState.DealerBust) return game.Wager;
            if (game.State == GameState.DealerWon || game.State == GameState.PlayerBust) return -game.Wager;
            if (game.State == GameState.PlayerWonNatural) return game.Wager * 1.5f;

            return 0.0f;
        }

        private int LogGameResultInDb(BlackjackGame game, string username, bool ignore = false)
        {
            BlackjackGameResult dbRecord = new BlackjackGameResult
            {
                Username = username,
                Wager = game.Wager,
                PlayerHand = String.Join("_", game.PlayerHand),
                PlayerScore = game.BestScore(game.PlayerHand),
                DealerHand = String.Join("_", game.DealerHand),
                DealerScore = game.BestScore(game.DealerHand),
                GameStateEnumId = (int)game.State,
                Ignore = ignore
            };

            try
            {
                using (BlackjackContext db = new BlackjackContext(_config.Get<string>("DbConnectionString")))
                {
                    db.GameResults.Add(dbRecord);
                    return db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{LP} Error logging GameResult to DB: {e.Message}\n{e.InnerException}");
                return 0;
            }
        }
        public bool ClearUserGame(string username)
        {
            Console.WriteLine($"{LP} Clearing game for {username}.");
            return ActiveGames.Remove(username);
        }

        public (string Response, ButtonGroup IncludedButtons) Bet(string username, uint wager)
        {
            BlackjackGame currentGame = GetUserCurrentGame(username);
            if (currentGame != null)
            {
                return ("Finish your current game first.", ButtonGroup.None);
            }

            //TODO: Check if user has sufficient funds for the wager.
            if (wager > _config.Get<uint>("BlackjackMaxWager"))
            {
                return ($"Maximum wager is {_config.Get<uint>("BlackjackMaxWager")}.", ButtonGroup.PlayAgain);
            }

            if (!StartGameForUser(username, wager))
            {
                return($"Could not create game for unknown reason.", ButtonGroup.None);
            }

            currentGame = GetUserCurrentGame(username);

            if (currentGame == null)
            {
                return ($"Supposedly the game was created but getting it returned null. Not great.", ButtonGroup.None);
            }

            Console.WriteLine($"{LP} Fetched created game, now calling first 'Tick()'.");
            currentGame.Tick();
            Console.WriteLine($"{LP} Finished the tick.");

            string response = currentGame.ToChatString();
            ButtonGroup includedButtons = ButtonGroup.HitStand;
            if (currentGame.State == GameState.PlayerWonNatural)
            {
                response += "You cheated and got a blackjack.\n";
                float credits = ProcessFinishedGame(username);
                response += $"Won {credits} credits.";
                includedButtons = ButtonGroup.PlayAgain;
            }
            else if (currentGame.State == GameState.Push)
            {
                response += $"It's a push. Your wager of {currentGame.Wager} is returned.";
                ProcessFinishedGame(username);
                includedButtons = ButtonGroup.PlayAgain;
            }

            return (response, includedButtons);
        }
        public (string Response, ButtonGroup IncludedButtons) Hit(string username)
        {
            Console.WriteLine($"{LP} Hit('{username}')");
            BlackjackGame currentGame = GetUserCurrentGame(username);
            if (currentGame == null)
            {
                return ($"Couldn't find an existing game for {username}. Start a new game by placing a bet.", ButtonGroup.PlayAgain);
            }

            try
            {
                currentGame.Tick(PlayerChoice.Hit);
            }
            catch (Exception e)
            {
                return (e.Message, ButtonGroup.None);
            }


            string playerHandString = String.Join(", ", currentGame.PlayerHand.Select(c => c.ToChatString()));
            ushort playerScore = currentGame.BestScore(currentGame.PlayerHand);

            switch (currentGame.State)
            {
                case GameState.PlayerChoose:
                    return ($"You drew {currentGame.PlayerHand.Last().ToChatString()}.\n\nYour hand: {playerHandString}\nHit or stand?", ButtonGroup.HitStand);
                case GameState.PlayerBust:
                    ProcessFinishedGame(username);  //TODO
                    return ($"You drew {currentGame.PlayerHand.Last().ToChatString()}\n\n" +
                        $"Busted with a score of {playerScore}. Final hand: {playerHandString}.\n" +
                        $"Lost {currentGame.Wager} credits.", ButtonGroup.PlayAgain);
                case GameState.DealerDraw:
                    string response = $"You drew {currentGame.PlayerHand.Last().ToChatString()}.\n\n" +
                        $"Hand: {playerHandString}. That's a perfect score of 21!\n";
                    while (currentGame.State == GameState.DealerDraw)
                    {
                        currentGame.Tick();
                        response += $"Dealer drew {currentGame.DealerHand.Last().ToChatString()}\n";
                    }

                    // BELOW HERE COMMENT OUT except final return
                    /*
                    string dealerHandString = String.Join(", ", currentGame.DealerHand.Select(c => c.ToChatString()));
                    ushort dealerScore = currentGame.BestScore(currentGame.DealerHand);

                    // The rest is kind of messy. We save data from the current game and process it before building the
                    // final response string, because this is cleaner than having ProcessGame() called with full exception
                    // handling in each case block.
                    GameState finalState = currentGame.State;
                    float wager = currentGame.Wager;
                    float? creditsChange = null;
                    string errorMsg = "";
                    SocketUser adminUser = _client.GetUser(_config.Get<ulong>("DiscordAdminUserId"));

                    try { creditsChange = ProcessFinishedGame(username); }
                    catch (Exception e) { errorMsg = $"{e.Message} {adminUser.Mention}"; }

                    StringBuilder sb = new StringBuilder();
                    switch (currentGame.State)
                    {
                        case GameState.DealerBust:
                            sb.AppendLine($"Dealer busted with a score of {dealerScore}. Final hand: {dealerHandString}");
                            sb.AppendLine(creditsChange.HasValue ? $"Won {creditsChange} credits." : $"ERROR: {errorMsg}");
                            break;
                        case GameState.PlayerWon:
                            sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"You won with a score of {playerScore}.");
                            sb.AppendLine(creditsChange.HasValue ? $"Won {creditsChange} credits." : $"ERROR: {errorMsg}");
                            break;
                        case GameState.DealerWon:
                            sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"You lost with a score of {playerScore}.");
                            sb.AppendLine(creditsChange.HasValue ? $"Lost {Math.Abs(creditsChange.Value)} credits." : $"ERROR: {errorMsg}");
                            break;
                        case GameState.Push:
                            sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                                $"It's a push.");
                            sb.AppendLine(creditsChange.HasValue ? $"Your wager of {currentGame.Wager} credits is returned." : $"ERROR: {errorMsg}");
                            break;
                        default:
                            response += $"BIG error. Not looking great. Unexpected GameState ({currentGame.State}) after " +
                                $"finishing dealer draws. Should be DealerBust({GameState.DealerBust}), PlayerWon(" +
                                $"{GameState.PlayerWon}), DealerWon({GameState.DealerWon}), or Push({GameState.Push}.";
                            break;
                    }
                    //ProcessFinishedGame(username);
                    */
                    return (ProcessAndGetResponse(currentGame, username), ButtonGroup.PlayAgain);
            }

            return ("Unusual phenomenon: fell out the switch without returning. Shouldn't happen.", ButtonGroup.None);
        }
        public (string Response, ButtonGroup IncludedButtons) Stand(string username)
        {
            BlackjackGame currentGame = GetUserCurrentGame(username);
            if (currentGame == null)
            {
                return ("Couldn't find an existing game. Start a new game by placing a bet.", ButtonGroup.PlayAgain);
            }

            try
            {
                currentGame.Tick(PlayerChoice.Stand);
            }
            catch (Exception e)
            {
                return (e.Message, ButtonGroup.None);
            }

            List<GameState> validStates = new List<GameState>() { GameState.DealerDraw, GameState.Push, GameState.DealerWon, GameState.PlayerWon };
            if (!validStates.Contains(currentGame.State))
            {
                Console.WriteLine($"{LP} Invalid game state after Tick on PlayerChoice.Stand: {currentGame.State}");
                ClearUserGame(username);
                return ($"There was a problem. Game aborted. (invalid state on tick, {currentGame.State})", ButtonGroup.None);
            }

            StringBuilder sb = new StringBuilder();
            while (currentGame.State == GameState.DealerDraw)
            {
                try
                {
                    currentGame.Tick();
                }
                catch (Exception e)
                {
                    return (e.Message, ButtonGroup.None);
                }
                sb.AppendLine($"Dealer drew {currentGame.DealerHand.Last().ToChatString()}.");
            }

            // BELOW HERE COMMENT OUT
            /*
            // DealerBust, PlayerWon, DealerWon, Push
            string dealerHandString = String.Join(", ", currentGame.DealerHand.Select(c => c.ToChatString()));
            ushort dealerScore = currentGame.BestScore(currentGame.DealerHand);
            ushort playerScore = currentGame.BestScore(currentGame.PlayerHand);

            // The rest is kind of messy. We save data from the current game and process it before building the
            // final response string, because this is cleaner than having ProcessGame() called with full exception
            // handling in each case block.
            GameState finalState = currentGame.State;
            float wager = currentGame.Wager;
            float? creditsChange = null;
            string errorMsg = "";
            SocketUser adminUser = _client.GetUser(_config.Get<ulong>("DiscordAdminUserId"));

            try { creditsChange = ProcessFinishedGame(username); }
            catch (Exception e) { errorMsg = $"{e.Message} {adminUser.Mention}"; }

            switch (finalState)
            {
                case GameState.DealerBust:
                    sb.AppendLine($"Dealer busted with a score of {dealerScore}. Final hand: {dealerHandString}");
                    sb.AppendLine(creditsChange.HasValue ? $"Won {creditsChange} credits." : $"ERROR: {errorMsg}");
                    break;
                case GameState.PlayerWon:
                    sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                        $"You won with a score of {playerScore}.");
                    sb.AppendLine(creditsChange.HasValue ? $"Won {creditsChange} credits." : $"ERROR: {errorMsg}");
                    break;
                case GameState.DealerWon:
                    sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                        $"You lost with a score of {playerScore}.");
                    sb.AppendLine(creditsChange.HasValue ? $"Lost {Math.Abs(creditsChange.Value)} credits." : $"ERROR: {errorMsg}");
                    break;
                case GameState.Push:
                    sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                        $"It's a push.");
                    sb.AppendLine(creditsChange.HasValue ? $"Your wager of {currentGame.Wager} credits is returned." : $"ERROR: {errorMsg}");
                    break;
                default:
                    sb.AppendLine($"BIG error. Not looking great. Unexpected GameState ({currentGame.State}) after " +
                        $"finishing dealer draws. Should be DealerBust({GameState.DealerBust}), PlayerWon(" +
                        $"{GameState.PlayerWon}), DealerWon({GameState.DealerWon}), or Push({GameState.Push}.");
                    break;
            }*/

            return (Response: ProcessAndGetResponse(currentGame, username), IncludedButtons: ButtonGroup.PlayAgain);
        }

        /// <summary>
        /// Runs ProcessFinishedGame on the provided game and returns a string to be sent to the Discord text channel.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private string ProcessAndGetResponse(BlackjackGame game, string username)
        {
            //GameState finalState = currentGame.State;
            //float wager = currentGame.Wager;
            SocketUser adminUser = _client.GetUser(_config.Get<ulong>("DiscordAdminUserId"));
            string dealerHandString = String.Join(", ", game.DealerHand.Select(c => c.ToChatString()));
            ushort dealerScore = game.BestScore(game.DealerHand);
            ushort playerScore = game.BestScore(game.PlayerHand);
            float? creditsChange = null;
            string errorMsg = "";

            try { creditsChange = ProcessFinishedGame(username); }
            catch (Exception e) { errorMsg = $"{e.Message} {adminUser.Mention}"; }

            StringBuilder sb = new StringBuilder();
            switch (game.State)
            {
                case GameState.DealerBust:
                    sb.AppendLine($"Dealer busted with a score of {dealerScore}. Final hand: {dealerHandString}");
                    sb.AppendLine(creditsChange.HasValue ? $"Won {creditsChange} credits." : $"ERROR: {errorMsg}");
                    break;
                case GameState.PlayerWon:
                    sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                        $"You won with a score of {playerScore}.");
                    sb.AppendLine(creditsChange.HasValue ? $"Won {creditsChange} credits." : $"ERROR: {errorMsg}");
                    break;
                case GameState.DealerWon:
                    sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                        $"You lost with a score of {playerScore}.");
                    sb.AppendLine(creditsChange.HasValue ? $"Lost {Math.Abs(creditsChange.Value)} credits." : $"ERROR: {errorMsg}");
                    break;
                case GameState.Push:
                    sb.AppendLine($"Dealer stood with a score of {dealerScore}. Final hand: {dealerHandString}\n" +
                        $"It's a push.");
                    sb.AppendLine(creditsChange.HasValue ? $"Your wager of {game.Wager} credits is returned." : $"ERROR: {errorMsg}");
                    break;
                default:
                    sb.AppendLine($"BIG error. Not looking great. Unexpected GameState ({game.State}) after " +
                        $"finishing dealer draws. Should be DealerBust({GameState.DealerBust}), PlayerWon(" +
                        $"{GameState.PlayerWon}), DealerWon({GameState.DealerWon}), or Push({GameState.Push}.");
                    break;
            }
            return sb.ToString();
        }
    }
}
