using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Blackjack
{
    [Group("blackjack", "We all love a game of BLACKJACK. Enjoy a game of Blackjack.")]
    public class SlashBlackjackCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("bet", "Start a new game with the chosen bet.")]
        public async Task bet([MinValue(1)] uint amount)
        {
            throw new NotImplementedException();
        }

        [SlashCommand("hit", "Take another card.")]
        public async Task hit()
        {
            throw new NotImplementedException();
        }

        [SlashCommand("stand", "Keep your current hand.")]
        public async Task stand()
        {
            throw new NotImplementedException();
        }

        [SlashCommand("show", "Show your current game.")]
        public async Task show()
        {
            throw new NotImplementedException();
        }
        
        
    }
}
