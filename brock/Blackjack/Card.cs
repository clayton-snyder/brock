using System;

namespace brock.Blackjack
{
    public class Card
    {
        private const string LP = "[BLACKJACK - Card]";  // Log prefix
        public enum CardColor
        {
            BLACK,
            RED
        }

        public enum CardSuit
        {
            SPADE,
            CLUB,
            DIAMOND,
            HEART
        }

        public enum CardValue
        {
            ACE,
            TWO,
            THREE,
            FOUR,
            FIVE,
            SIX,
            SEVEN,
            EIGHT,
            NINE,
            TEN,
            JACK,
            QUEEN,
            KING
        }

        public CardSuit Suit;
        public CardValue Value;

        public Card(CardSuit suit, CardValue value)
        {
            Suit = suit;
            Value = value;
        }

        public CardColor Color()
        {
            if (Suit == CardSuit.SPADE || Suit == CardSuit.CLUB) return CardColor.BLACK;
            else return CardColor.RED;
        }

        /// <summary>
        /// We give ACE=11 by default to allow greedy summation in the logic that calculates a 
        /// hand's score. Such logic is responsible for considering the possibility of ACE=1.
        /// </summary>
        public ushort Score()
        {
            switch (Value)
            {
                case CardValue.ACE:
                    return 11;
                case CardValue.TWO:
                    return 2;
                case CardValue.THREE:
                    return 3;
                case CardValue.FOUR:
                    return 4;
                case CardValue.FIVE:
                    return 5;
                case CardValue.SIX:
                    return 6;
                case CardValue.SEVEN:
                    return 7;
                case CardValue.EIGHT:
                    return 8;
                case CardValue.NINE:
                    return 9;
                case CardValue.TEN:
                    return 10;
                case CardValue.JACK:
                    return 10;
                case CardValue.QUEEN:
                    return 10;
                case CardValue.KING:
                    return 10;
                default:
                    throw new InvalidOperationException($"{LP} Unknown CardValue: {Value}");
            }
        }

        public override string ToString()
        {
            string cardString = "";
            switch (Value)
            {
                case CardValue.ACE:
                    cardString = "A";
                    break;
                case CardValue.TWO:
                    cardString = "2";
                    break;
                case CardValue.THREE:
                    cardString = "3";
                    break;
                case CardValue.FOUR:
                    cardString = "4";
                    break;
                case CardValue.FIVE:
                    cardString = "5";
                    break;
                case CardValue.SIX:
                    cardString = "6";
                    break;
                case CardValue.SEVEN:
                    cardString = "7";
                    break;
                case CardValue.EIGHT:
                    cardString = "8";
                    break;
                case CardValue.NINE:
                    cardString = "9";
                    break;
                case CardValue.TEN:
                    cardString = "10";
                    break;
                case CardValue.JACK:
                    cardString = "J";
                    break;
                case CardValue.QUEEN:
                    cardString = "Q";
                    break;
                case CardValue.KING:
                    cardString = "K";
                    break;
                default:
                    cardString = "?";
                    break;
            }

            switch (Suit)
            {
                case CardSuit.SPADE:
                    cardString += "S";
                    break;
                case CardSuit.CLUB:
                    cardString += "C";
                    break;
                case CardSuit.DIAMOND:
                    cardString += "D";
                    break;
                case CardSuit.HEART:
                    cardString += "H";
                    break;
                default:
                    cardString += "?";
                    break;
            }

            return cardString;
        }
    }
}
