using brock.DbModels;
using Discord.WebSocket;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Timers;
using System.Web;

namespace brock.Services
{
    public class ScienceService
    {
        private HttpClient Client;
        private Timer _checkIfPostTimer;
        private ConfigService _config;
        private DiscordSocketClient _discord;
        private readonly string baseUrl = "https://todayinsci.com";
        private const string LP = "[ScienceService]";  // Log prefix
        private DateTime? _nextPostAt;
        private List<KeyValuePair<ulong, ulong>> _guildToChannelIdsToPost;

        public ScienceService(ConfigService config, DiscordSocketClient discord)
        {
            _config = config;
            _discord = discord;
        }

        public void Initialize()
        {
            Client = new HttpClient();
            _nextPostAt = GetNextPostTimeFromDb();
            Console.WriteLine($"{LP} Initialized _nextPostAt to: {_nextPostAt.Value} " +
                $"(UtcNow is {DateTime.UtcNow}, regular Now is {DateTime.Now}");
            // TODO: Create the scheduler
            //https://stackoverflow.com/questions/19291816/executing-method-every-hour-on-the-hour
            // Check every 15 minutes if it's time for the daily fact yet
            _checkIfPostTimer = new Timer(_config.Get<int>("SciencePostCheckTimerIntervalMs"));
            _checkIfPostTimer.AutoReset = true;
            _checkIfPostTimer.Elapsed += CheckIfPostTimerElapsed;
            _checkIfPostTimer.Start();

            string sciencePostChannelName = _config.Get<string>("SciencePostChannelName");
            _guildToChannelIdsToPost = new List<KeyValuePair<ulong, ulong>>();
            foreach (SocketGuild guild in _discord.Guilds)
            {
                Console.WriteLine($"{LP} CHECKING GUILD {guild.Name}, # text channels = {guild.TextChannels.Count}");
                foreach (SocketTextChannel textChannel in guild.TextChannels)
                {
                    Console.WriteLine($"{LP} Checking if \"{textChannel.Name}\" matches \"{sciencePostChannelName}\"");
                    if (textChannel.Name.Equals(sciencePostChannelName))
                    {
                        _guildToChannelIdsToPost.Add(new KeyValuePair<ulong, ulong>(guild.Id, textChannel.Id));
                        Console.WriteLine($"{LP} Added guild-to-channel ID mapping: {guild.Id}, {textChannel.Id}");
                        break;
                    }
                }
            }
        }

        private async void CheckIfPostTimerElapsed(Object source, ElapsedEventArgs eventArgs)
        {
            Console.WriteLine($"{LP} _checkIfPostTimer elapsed.");
            if (!_nextPostAt.HasValue)
            {
                Console.WriteLine($"{LP} _nextPostAt was found null (weird...), populating from DB.");
                _nextPostAt = GetNextPostTimeFromDb();
            }

            // Since we check every 15 mins, subtract timerIntervalMs/2 so the post time doesn't slowly inch forward
            int timerIntervalMs = _config.Get<int>("SciencePostCheckTimerInvervalMs");
            if (_nextPostAt.Value.AddMilliseconds(-timerIntervalMs / 2) < DateTime.UtcNow)
            {
                ushort month = (ushort)DateTime.Now.Month, day = (ushort)DateTime.Now.Day;
                try
                {
                    KeyValuePair<string, string> scienceEvent = await GetScienceEventForDay(month, day);
                    int postedToCount = PostScienceEvent(scienceEvent);
                    Console.WriteLine($"{LP} Successfully posted to {postedToCount} {(postedToCount == 1 ? "channel" : "channels")}!");
                    _nextPostAt = DateTime.UtcNow.AddDays(1);
                    int dbRecordsAdded = LogDailyPostInDb(month, day, scienceEvent.Key, scienceEvent.Value, DateTime.UtcNow);
                    Console.WriteLine($"{LP} Result from logging post event to DB (should be 1): {dbRecordsAdded} \n" +
                        $"New _nextPostAt: {_nextPostAt} (UtcNow is {DateTime.UtcNow}, regular Now is {DateTime.Now}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{LP} Error fetching or posting science event for month/day " +
                        $"{DateTime.Now.Month}/{DateTime.Now.Day}: {e.Message}\n{e.InnerException}");
                }
            }
            else
            {
                Console.WriteLine($"{LP} Not time yet. Next post at {_nextPostAt.Value} local, {_nextPostAt.Value.ToUniversalTime()} UTC");
            }
        }

        // Posts the provided text to all channels in channelIdsToPost
        private int PostScienceEvent(KeyValuePair<string, string> dailyEvent)
        {
            string postText = $"_**{dailyEvent.Key}**_\n{dailyEvent.Value}";
            Console.WriteLine($"{LP} Trying to post today's science event:\n{postText}");
            int channelsPosted = 0;
            foreach (KeyValuePair<ulong, ulong> guildToChannel in _guildToChannelIdsToPost)
            {
                try
                {
                    _discord.GetGuild(guildToChannel.Key).GetTextChannel(guildToChannel.Value).SendMessageAsync(postText);
                    Console.WriteLine($"{LP} Posted daily science event to {guildToChannel.Key}, {guildToChannel.Value}");
                    channelsPosted++;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{LP} Failed to send daily science event to ({guildToChannel.Key}, {guildToChannel.Value}): {e.Message}");
                }
            }
            return channelsPosted;
        }

        private DateTime GetNextPostTimeFromDb()
        {
            try
            {
                using (DailySciencePostContext db = new DailySciencePostContext(_config.Get<string>("DbConnectionString")))
                {
                    var query = from post in db.DailySciencePost 
                                where !post.Ignore
                                orderby post.PostedAtUtc descending 
                                select post.PostedAtUtc;

                    DateTime lastPostTime = query.First();
                    return lastPostTime.AddDays(1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{LP} Error querying DB for most recent Science post: {e.Message}\n{e.InnerException}\n" +
                    $"Returning DateTime.MinValue");
                return DateTime.MinValue;
            }
        }

        public int LogDailyPostInDb(ushort month, ushort day, string title, string description, DateTime postedAt)
        {
            try
            {
                Console.WriteLine($"{LP} Attempting to open DB connection...");
                using (DailySciencePostContext db = new DailySciencePostContext(_config.Get<string>("DbConnectionString")))
                {
                    DailySciencePost post = new DailySciencePost
                    {
                        Month = month,
                        Day = day,
                        Title = title,
                        Description = description,
                        PostedAtUtc = postedAt.ToUniversalTime(),
                        CreatedDateUtc = DateTime.UtcNow
                    };
                    db.DailySciencePost.Add(post);
                    return db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{LP} Error logging post to DB: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
                return 0;
            }
        }

        public async Task<KeyValuePair<string, string>> GetScienceEventForDay(int month, int day)
        {
            if (month < 1 || month > 12) return new KeyValuePair<string, string>("Error", $"Invalid month value: {month}");
            if (Array.Exists(new ushort[] { 1, 3, 5, 7, 8, 10, 12 }, e => e == month) && day > 31)
                return new KeyValuePair<string, string>("Error", $"Invalid day value ({day}) for month {month}.");
            if (Array.Exists(new ushort[] { 4, 6, 9, 11 }, e => e == month) && day > 30)
                return new KeyValuePair<string, string>("Error", $"Invalid day value ({day}) for month {month}.");
            if (month == 2 && day > 28)
                return new KeyValuePair<string, string>("Error", $"Invalid day value ({day}) for month {month} (February?).");

            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:102.0) Gecko/20100101 Firefox/102.0");
            string requestUrl = $"{baseUrl}/{month}/{month}_{day}.htm";
            Console.WriteLine($"{LP} Performing GET: {requestUrl}");
            string rawHtml = "";
            try
            {
                rawHtml = await Client.GetStringAsync(requestUrl);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{LP} Exception sending HTTP request: {e.Message}");
                return new KeyValuePair<string, string>("Error", $"Error sending HTTP request: {e.Message}");
            }
            Console.WriteLine($"{LP} Returned.");

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHtml);

            HtmlNode mainColumnNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='maincolumn']");
            if (mainColumnNode == null)
            {
                Console.WriteLine($"{LP} Couldn't find 'maincolumn' div in HTML.");
                return new KeyValuePair<string, string>("Error", $"Couldn't find 'maincolumn' div in HTML. requestUrl={requestUrl}");
            }

            HtmlNodeCollection subHeadingNodes = mainColumnNode.SelectNodes("//div[@class='daysubheading']");
            HtmlNode listContainerNode = null;
            foreach (HtmlNode subHeadingNode in subHeadingNodes)
            {
                //Console.WriteLine(subHeadingNode.InnerText);
                if (subHeadingNode.InnerText.ToUpper().Contains("EVENTS"))
                {
                    //Console.WriteLine($"FOUND: {subHeadingNode.OuterHtml}");
                    listContainerNode = subHeadingNode;
                    
                    break;
                }
            }
            if (listContainerNode == null)
            {
                Console.WriteLine($"{LP} Couldn't find 'daysubheading' child of 'maincolumn' div with 'EVENTS' InnerText.");
                return new KeyValuePair<string, string>
                    ("Error", $"No 'daysubheading' child of 'maincolumn' div with 'EVENTS' in InnerText. requestUrl={requestUrl}");
            }

            Dictionary<string, string> options = new Dictionary<string, string>();
            HtmlNode sib = listContainerNode.NextSibling;
            while (sib != null)
            {
                Console.WriteLine($"{LP} Node: {sib.InnerText}");
                string title = (HttpUtility.HtmlDecode(sib.InnerText)).Trim();
                sib = sib.NextSibling;
                Console.WriteLine($"{LP} Node: {sib.InnerText}");
                string description = (HttpUtility.HtmlDecode(sib.InnerText)).Trim();
                sib = sib.NextSibling;

                // Dodges the empty nodes
                if (!(string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description)))
                {
                    options.Add(title, description);
                }
            }

            Random rand = new Random();
            KeyValuePair<string, string> selectedEvent = options.ElementAt(rand.Next(0, options.Count));


            return new KeyValuePair<string, string>(selectedEvent.Key, selectedEvent.Value);
        }
    }
}
