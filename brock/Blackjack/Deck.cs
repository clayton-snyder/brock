using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Blackjack
{
    internal class Deck
    {
        // If this class is ever going to be used outside of blackjack, a Queue might not be good enough.
        // You probably would just need a list or custom Deque implementation.
        Queue<Card> cards;

        public Deck()
        {
            ShuffleNew();
        }


        public Card Draw()
        {
            return cards.Dequeue();
        }

        /// <summary>
        /// NOTE: This does not check if the deck will be valid after adding the card!
        /// </summary>
        /// <param name="card"></param>
        public void addToBottom(Card card)
        {
            cards.Enqueue(card);
        }

        /// <summary>
        /// Shuffles the remaining cards in the deck.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void ShuffleRemaining()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Resets the deck with a new shuffled set of 52 cards.
        /// </summary>
        public void ShuffleNew()
        {
            List<Card> orderedCards = new List<Card>();
            foreach (Card.CardSuit suit in Enum.GetValues(typeof(Card.CardSuit)))
            {
                foreach(Card.CardValue value in Enum.GetValues(typeof(Card.CardValue)))
                {
                    orderedCards.Add(new Card(suit, value));
                }
            }

            Random rand = new Random();
            this.cards = new Queue<Card>(orderedCards.OrderBy(card => rand.Next()));

            Console.WriteLine($"[BLACKJACK - Deck] Deck shuffled. cards.Count={cards.Count}, cards.Peek()={cards.Peek()}");
        }
    }
}
