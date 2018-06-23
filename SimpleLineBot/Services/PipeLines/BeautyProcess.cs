using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Line;

namespace SimpleLineBot.Services.PipeLines {
    public class BeautyProcess : IPipeLineProcess {
        public ILineBot Bot { get; set; }

        public BeautyProcess(ILineBot bot) {
            Bot = bot;
        }

        public async Task<bool> Handle(ILineEvent e) {
            if (e.EventType != LineEventType.Message) return false;

            if (e.Message?.Text?.IndexOf("妹子") != 0) return false;

            HttpClient client = new HttpClient();
            Uri image = null;
            while (image == null) {
                var url = await GetImageUrl();
                if (url != null) {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) {
                        image = new Uri(url);
                    }
                }
            }

            await Bot.Reply(e.ReplyToken, new ImageMessage() {
                Url = image,
                PreviewUrl = image
            });

            return true;
        }

        public async Task<string> GetImageUrl() {
            HttpClient client = new HttpClient();

            var mainPage = new HtmlDocument();

            mainPage.LoadHtml(
                await client.GetStringAsync("https://www.ptt.cc/bbs/Beauty/index.html")
                );

            Regex pageUrl = new Regex("/bbs/Beauty/index\\d+\\.html");
            Regex number = new Regex("\\d+");

            int maxPages = int.Parse(
                number.Match(
                    mainPage.DocumentNode
                        .SelectNodes("//a")
                        .Where(x => pageUrl.IsMatch(x.Attributes["href"]?.Value ?? ""))
                        .OrderBy(x => x.Attributes["href"].Value.Length).Last()
                .Attributes["href"].Value).Value ?? ""
            );

            Random rand = new Random((int)DateTime.Now.Ticks);

            //隨便挑一頁
            var targetPage = rand.Next(maxPages - 1000, maxPages);

            async Task<string> GetRandomImgurUrl(string url) {
                var tempPage = new HtmlDocument();

                tempPage.LoadHtml(
                    await client.GetStringAsync(url)
                );

                Regex post = new Regex(@"/bbs/Beauty/M\..+\.html");

                var posts = tempPage.DocumentNode
                        .SelectNodes("//a")
                        .Where(x => post.IsMatch(x.Attributes["href"]?.Value ?? ""))
                        .Select(x => x.Attributes["href"].Value)
                        .ToArray();

                tempPage.LoadHtml(
                    await client.GetStringAsync("https://www.ptt.cc" + posts[rand.Next(0, posts.Length)])
                );

                Regex imgur = new Regex(@"https?:\/\/i\.imgur\.com\/.*");

                var urls = tempPage.DocumentNode
                        .SelectNodes("//a")
                        .Where(x => imgur.IsMatch(x.Attributes["href"]?.Value ?? ""))
                        .Select(x => x.Attributes["href"].Value)
                        .ToArray();

                return urls[rand.Next(0, urls.Length)];
            }

            try {
                var result = await GetRandomImgurUrl($"https://www.ptt.cc/bbs/Beauty/index{targetPage}.html");

                return result.Replace("http://", "https://");
            } catch {
                return null;
            }
        }
    }
}
