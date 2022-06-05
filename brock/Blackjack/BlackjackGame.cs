using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Blackjack
{
    internal class BlackjackGame
    {
        /// <summary>
        /// None means the current state of the game is not waiting for player input.
        /// Basically that they have "stood" or busted on a previous tick.
        /// </summary>
        public enum PlayerChoice
        {
            None,
            Stand,
            Hit,
        }

        /// <summary>
        /// PlayerWon and DealerWon imply a result based on hand score (i.e., not due to a bust).
        /// </summary>
        // Theoretically these are in order of possibility. I.e., If the player busted, the state 
        // could never go back to PlayerChoose. If the dealer is drawing, it means the player 
        // stood without busting. 
        public enum GameState
        {
            Start,
            PlayerChoose,
            PlayerBust,
            DealerDraw,
            DealerBust,
            PlayerWon,
            PlayerWonNatural,
            DealerWon,
            Push,
        }

        public Deck Deck;
        public List<Card> PlayerHand;
        public List<Card> DealerHand;
        public uint Wager;
        public GameState State;

        public BlackjackGame(uint Wager)
        {
            this.Wager = Wager;
            this.Deck = new Deck(); // Default Deck constructor shuffles itself
        }

        /// <summary>
        /// Argument should only be provided when State is PlayerChoose.
        /// </summary>
        /// <param name="playerChoice"></param>
        public void Tick(PlayerChoice playerChoice = PlayerChoice.None)
        {
            switch (State)
            {
                case GameState.Start:
                    // Deal cards to player and dealer
                    PlayerHand.Add(Deck.Draw());
                    DealerHand.Add(Deck.Draw());
                    PlayerHand.Add(Deck.Draw());
                    DealerHand.Add(Deck.Draw());

                    // if player has blackjack
                    //      if dealer has blackjack, State=Push
                    //      else State=PlayerWonNatural
                    if (BestScore(PlayerHand) == 21) 
                        State = BestScore(DealerHand) == 21 ? GameState.Push : GameState.PlayerWonNatural;
                    else 
                        State = GameState.PlayerChoose;

                    break;

                case GameState.PlayerChoose:
                // if playerChocie == None, Error!!
                // if playerChoice == Stand, State=DealerDraw, break
                // if playerChoice == Hit draw for player
                // calculate player score
                // if playerScore<21, State=PlayerChoose, break
                // if playerScore==21, State=DealerDraw, break
                // if playerScore>21, State=PlayerBust, break
                case GameState.DealerDraw:
                // draw for dealer
                // if dealerScore<17, State=DealerDraw, break
                // if dealerScore>21, State=DealerBust, break
                // if playerScore>dealerScore, State=PlayerWon
                // elif dealerScore>playerScore, State=DealerWon
                // else State=Push
                default:
                    // BAD GAME STATE MER
                    Console.WriteLine($"[BLACKJACK - BlackjackGame] tick() called on invalid state: {State}. " +
                        $"tick() should only be called on states Start({GameState.Start}, " +
                        $"PlayerChoose({GameState.PlayerChoose}), or DealerDraw({GameState.DealerDraw}.");
                    throw new InvalidOperationException($"tick() called on invalid state: {State}");
            }
        }

        public ushort BestScore(List<Card> hand)
        {
            ushort score = 0, aces = 0;
            foreach (Card card in hand)
            {
                score += (ushort)card.Value;
                if (card.Value == Card.CardValue.ACE) aces++;
            }

            while (score > 21 && aces > 0)
            {
                score -= 10;
                aces--;
            }

            return score;
        }
    }
}
