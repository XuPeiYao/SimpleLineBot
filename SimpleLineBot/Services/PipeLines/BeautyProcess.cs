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

            var command = e.Message?.Text.Split(' ');

            HttpClient client = new HttpClient();
            Uri image = null;
            int tryLimit = 10;

            if (command.Length == 1) {
                while (image == null && tryLimit > 0) {
                    var url = await GetImageUrl();
                    if (url != null) {
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode) {
                            image = new Uri(url);
                        }
                    }
                    tryLimit--;
                }
            } else {
                while (image == null && tryLimit > 0) {
                    var url = await GetImageUrlByKeyword(command[1]);
                    if (url != null) {
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode) {
                            image = new Uri(url);
                        }
                    }
                    tryLimit--;
                }
            }

            if (image == null) {
                await Bot.Reply(e.ReplyToken,
                    new TextMessage() {
                        Text = "對不起!!現在找不到你要的妹子"
                    }, new ImageMessage() {
                        Url = new Uri("https://img.moegirl.org/common/c/c4/Owabi.jpg"),
                        PreviewUrl = new Uri("https://img.moegirl.org/common/c/c4/Owabi.jpg")
                    });
            } else {
                await Bot.Reply(e.ReplyToken, new ImageMessage() {
                    Url = image,
                    PreviewUrl = image
                });
            }
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

                var pushs = tempPage.DocumentNode.SelectNodes("//div[contains(@class, 'push')]");
                foreach (var push in pushs) {
                    push.Remove();
                }

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

        public async Task<string> GetImageUrlByKeyword(string keyword) {
            HttpClient client = new HttpClient();

            var mainPage = new HtmlDocument();

            mainPage.LoadHtml(
                await client.GetStringAsync("https://www.ptt.cc/bbs/Beauty/search?page=1&q=" + Uri.EscapeDataString(keyword))
                );

            Regex number = new Regex("\\d+");

            int maxPages = int.Parse(
                number.Match(
                    mainPage.DocumentNode
                        .SelectNodes("//a")
                        .Where(x => x.InnerText == "最舊")
                        .OrderBy(x => x.Attributes["href"].Value.Length).Last()
                .Attributes["href"].Value).Value ?? ""
            );

            Random rand = new Random((int)DateTime.Now.Ticks);

            //隨便挑一頁
            var targetPage = rand.Next(1, maxPages + 1);

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

                var pushs = tempPage.DocumentNode.SelectNodes("//div[contains(@class, 'push')]");
                foreach (var push in pushs) {
                    push.Remove();
                }

                Regex imgur = new Regex(@"https?:\/\/i\.imgur\.com\/.*");

                var urls = tempPage.DocumentNode
                        .SelectNodes("//a")
                        .Where(x => imgur.IsMatch(x.Attributes["href"]?.Value ?? ""))
                        .Select(x => x.Attributes["href"].Value)
                        .ToArray();

                return urls[rand.Next(0, urls.Length)];
            }

            try {
                var result = await GetRandomImgurUrl($"https://www.ptt.cc/bbs/Beauty/search?page={targetPage}&q=" + Uri.EscapeDataString(keyword));

                return result.Replace("http://", "https://");
            } catch {
                return null;
            }
        }

    }
}
