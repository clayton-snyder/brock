using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            string rollsString = String.Join(", ", results);

            if (plaintext)
            {
                await RespondAsync($"Total: {total}, Rolls: {rollsString}");
                return;
            }

            //var groupedRolls = results.GroupBy(v => v).OrderByDescending(g => g.Count());
            //string mode = $"{groupedRolls.First().Key} rolled {groupedRolls.First().Count()} times.";

            var summaryEmbed = new EmbedBuilder { Title = $"{Context.User.Username}'s Dice Roll Results" };
            summaryEmbed.AddField("Total", results.Sum());
            summaryEmbed.AddField("Max / Min roll", $"{results.Max()} / {results.Min()}");
            summaryEmbed.AddField("Rolls", String.Join(", ", results));
            summaryEmbed.WithCurrentTimestamp().WithColor(Color.Red);
            await RespondAsync(embed: summaryEmbed.Build());
        }
    }
}
