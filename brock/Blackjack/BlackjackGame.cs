﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Blackjack
{
    public class BlackjackGame
    {
        private const ushort TargetScore = 21;
        private const ushort DealerStandScore = 17;
        private const string LP = "[BLACKJACK - BlackjackGame]";  // Log prefix

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
            this.State = GameState.Start; // Necessary?
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
                    PlayerHand.Add(Deck.Draw());
                    DealerHand.Add(Deck.Draw());
                    PlayerHand.Add(Deck.Draw());
                    DealerHand.Add(Deck.Draw());

                    if (BestScore(PlayerHand) == TargetScore) 
                        State = (BestScore(DealerHand) == TargetScore ? GameState.Push : GameState.PlayerWonNatural);
                    else 
                        State = GameState.PlayerChoose;

                    break;


                case GameState.PlayerChoose:
                    if (playerChoice == PlayerChoice.None)
                    {
                        string noChoiceMsg = $"{LP} State is PlayerChoose but playerChoice is None.";
                        Console.WriteLine(noChoiceMsg);
                        throw new ArgumentException(noChoiceMsg);
                    }

                    if (playerChoice == PlayerChoice.Stand)
                        State = GameState.DealerDraw;
                    else if (playerChoice == PlayerChoice.Hit)
                    {
                        PlayerHand.Add(Deck.Draw());
                        ushort playerScore = BestScore(PlayerHand);

                        if (playerScore < TargetScore) State = GameState.PlayerChoose;
                        if (playerScore == TargetScore) State = GameState.DealerDraw;
                        if (playerScore > TargetScore) State = GameState.PlayerBust;
                    }
                    else
                    {
                        string unknownChoiceMsg = $"{LP} Unknown PlayerChoice: {playerChoice}";
                        Console.WriteLine(unknownChoiceMsg);
                        throw new ArgumentException(unknownChoiceMsg);
                    }

                    break;
                

                case GameState.DealerDraw:
                    DealerHand.Add(Deck.Draw());
                    ushort dealerScore = BestScore(DealerHand);

                    if (dealerScore > TargetScore) 
                        State = GameState.DealerBust;
                    else if (dealerScore < DealerStandScore) 
                        State = GameState.DealerDraw;
                    else
                    {
                        ushort playerScore = BestScore(PlayerHand);

                        if (playerScore > dealerScore)
                            State = GameState.PlayerWon;
                        else if (dealerScore > playerScore)
                            State = GameState.DealerWon;
                        else
                            State = GameState.Push;
                    }

                    break;


                default:
                    string badStateMsg = $"{LP} tick() called on invalid state: {State}. " +
                        $"tick() should only be called on states Start({GameState.Start}, " +
                        $"PlayerChoose({GameState.PlayerChoose}), or DealerDraw({GameState.DealerDraw}.";
                    Console.WriteLine(badStateMsg);
                    throw new InvalidOperationException(badStateMsg);
            }
        }

        public ushort BestScore(List<Card> hand)
        {
            ushort score = 0, aces = 0;
            foreach (Card card in hand)
            {
                score += card.Score();
                if (card.Value == Card.CardValue.ACE) aces++;
            }

            while (score > TargetScore && aces > 0)
            {
                score -= 10;
                aces--;
            }

            return score;
        }
    }
}
