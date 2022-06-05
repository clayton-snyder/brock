namespace brock.Blackjack
{
    public class Card
    {
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

        /// <summary>
        /// We give ACE=11 by default to allow greedy summation in the logic that calculates a 
        /// hand's score. Such logic is responsible for considering the possibility of ACE=1.
        /// </summary>
        public enum CardValue : ushort
        {
            ACE = 11,
            TWO = 2,
            THREE = 3,
            FOUR = 4,
            FIVE = 5,
            SIX = 6,
            SEVEN = 7,
            EIGHT = 8,
            NINE = 9,
            TEN = 10,
            JACK = 10,
            QUEEN = 10,
            KING = 10
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
