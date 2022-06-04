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
