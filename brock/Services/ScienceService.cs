﻿using brock.DbModels;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web;

namespace brock.Services
{
    public class ScienceService
    {
        private HttpClient Client;
        private Timer _timer;
        private readonly string baseUrl = "https://todayinsci.com";
        private const string LP = "[ScienceService]";  // Log prefix
        private DateTime? _nextPostAt;

        public void Initialize()
        {
            Client = new HttpClient();
            _nextPostAt = null;
            // TODO: Create the scheduler
            //https://stackoverflow.com/questions/19291816/executing-method-every-hour-on-the-hour
            // Check every 15 minutes if it's time for the daily fact yet
            _timer = new Timer(15 * 60 * 1000); // 
            //timer.Elapsed += do thing
        }

        //public bool IsDailyEventTime()
        //{

        //}

        public int LogDailyPostInDb(ushort month, ushort day, string title, string description, DateTime postedAt)
        {
            try
            {
                Console.WriteLine("Attempting to open DB connection...");
                using (DailySciencePostContext db = new DailySciencePostContext())
                {
                    DailySciencePost post = new DailySciencePost
                    {
                        Month = month,
                        Day = day,
                        Title = title,
                        Description = description,
                        PostedAt = postedAt.ToUniversalTime(),
                        CreatedDate = DateTime.UtcNow
                    };
                    db.DailySciencePost.Add(post);
                    return db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error logging post to DB: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
                return 0;
            }
        }

        public async Task<string> GetScienceEventForDay(int month, int day)
        {
            if (month < 1 || month > 12) return $"Invalid month value: {month}";
            if (Array.Exists(new ushort[] { 1, 3, 5, 7, 8, 10, 12 }, e => e == month) && day > 31)
                return $"Invalid day value ({day}) for month {month}.";
            if (Array.Exists(new ushort[] { 4, 6, 9, 11 }, e => e == month) && day > 30)
                return $"Invalid day value ({day}) for month {month}.";
            if (month == 2 && day > 28)
                return $"Invalid day value ({day}) for month {month} (February?).";

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
                return "Error sending HTTP request.";
            }
            Console.WriteLine($"{LP} Returned.");

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHtml);

            HtmlNode mainColumnNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='maincolumn']");
            if (mainColumnNode == null)
            {
                Console.WriteLine($"{LP} Couldn't find 'maincolumn' div in HTML.");
                return $"Couldn't find 'maincolumn' div in HTML. requestUrl={requestUrl}";
            }

            HtmlNodeCollection subHeadingNodes = mainColumnNode.SelectNodes("//div[@class='daysubheading']");
            HtmlNode listContainerNode = null;
            foreach (HtmlNode subHeadingNode in subHeadingNodes)
            {
                Console.WriteLine(subHeadingNode.InnerText);
                if (subHeadingNode.InnerText.ToUpper().Contains("EVENTS"))
                {
                    Console.WriteLine($"FOUND: {subHeadingNode.OuterHtml}");
                    listContainerNode = subHeadingNode;
                    
                    break;
                }
            }
            if (listContainerNode == null)
            {
                Console.WriteLine($"{LP} Couldn't find 'daysubheading' child of 'maincolumn' div with 'EVENTS' InnerText.");
                return $"Couldn't find 'daysubheading' child of 'maincolumn' div with 'EVENTS' in InnerText. requestUrl={requestUrl}";
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


            return $"_**{selectedEvent.Key}**_\n{selectedEvent.Value}";
        }
    }
}
