using Discord;
using Discord.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace brock.Services
{
    public class SlashUtilCmdsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly Random random = new Random();

        [SlashCommand("coinflip", "Flip a coin.")]
        public async Task Flip()
        {
            await RespondAsync(random.Next(2) == 1 ? "Heads" : "Tails");
        }

        [SlashCommand("diceroll", "Roll some dice.")]
        public async Task DiceRoll(
            [MinValue(1)] [MaxValue(100)] [Summary(description:"How many dice to roll?")] int rolls, 
            [MinValue(1)] [MaxValue(UInt32.MaxValue)] [Summary(description:"How many sides on a die?")] int sides,
            [Summary(description:"Omit embed and output as plain text.")] bool plaintext = false)
        {
            if (rolls < 1 || rolls > 100 || sides < 1)
            {
                await RespondAsync("Invalid parameters.");
                return;
            }

            int[] results = new int[rolls];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = random.Next(1, sides);
            }

            int total = results.Sum();
            string avgFormatted = ((double)total / rolls).ToString($"F{2}");
            string rollsString = String.Join(", ", results);

            if (plaintext)
            {
                await RespondAsync($"{Context.User.Username} rolled {rolls} {sides}-sided dice. **Total**: {total} ({avgFormatted} avg), **Rolls**: {rollsString}");
                return;
            }

            var summaryEmbed = new EmbedBuilder { Title = $"{Context.User.Username}'s Dice Roll Results" };
            summaryEmbed.Description = $"{Context.User.Username} rolled {rolls} {sides}-sided dice.";
            summaryEmbed.AddField("Total", $"{total} ({avgFormatted} avg)");
            summaryEmbed.AddField("Highest / lowest roll", $"{results.Max()} / {results.Min()}");
            summaryEmbed.AddField("Rolls", String.Join(", ", results));
            summaryEmbed.WithCurrentTimestamp().WithColor(Color.Red);
            await RespondAsync(embed: summaryEmbed.Build());
        }
    }
}
