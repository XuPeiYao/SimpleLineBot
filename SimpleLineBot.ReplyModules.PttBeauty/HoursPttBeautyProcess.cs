using HtmlAgilityPack;
using Line;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleLineBot.ReplyModules.PttBeauty {
    class HoursPttBeautyProcess : ILineReplyModule {
        public static ILineBot Bot { get; set; }

        public Uri PostUrl { get; set; }
        public string Title { get; set; }

        public Task Loop { get; set; }
        public CancellationTokenSource Token { get; set; }

        public static List<string> To { get; set; } = new List<string>();

        public HoursPttBeautyProcess(ILineBot bot) {
            Bot = bot;
        }

        public async Task<bool> Handle(ILineEvent e) {
            if (e.EventType != LineEventType.Message) return false;

            switch (e.Message?.Text?.Trim()) {
                case "開始每小時一個妹子":
                    Token = new CancellationTokenSource();
                    CancellationToken ct = Token.Token;
                    To.Add(e.Source.Group?.Id ?? e.Source.Room?.Id ?? e.Source.User?.Id);
                    Loop = Task.Factory.StartNew(async () => {
                        while (true) {
                            HttpClient client = new HttpClient();
                            Uri image = null;

                            Console.WriteLine("正在取得照片");

                            while (image == null) {
                                var url = await GetImageUrl();
                                if (url != null) {
                                    var response = await client.GetAsync(url);
                                    if (response.IsSuccessStatusCode) {
                                        image = new Uri(url);
                                    }
                                }
                            }

                            Console.WriteLine(image);

                            ISendMessage message = new ImageMessage() {
                                Url = image,
                                PreviewUrl = image
                            };

                            foreach (var target in To) {
                                Console.WriteLine(target);
                                try {
                                    await Bot.Push(target, message);
                                } catch (Exception ex) {
                                    Console.WriteLine(ex.ToString());
                                }
                            }

                            if (ct.IsCancellationRequested) {
                                break;
                            }

                            Thread.Sleep(10000);
                        }
                    }, ct);
                    break;
                case "結束每小時一個妹子":
                    To.Remove(e.Source.Group?.Id ?? e.Source.Room?.Id ?? e.Source.User?.Id);
                    if (Token == null && To.Count > 0) break;
                    Token.Cancel();
                    break;
                default:
                    return false;
            }

            await Bot.Reply(e.ReplyToken, new TextMessage("已經列入排程"));

            return true;
        }

        public static async Task<string> GetImageUrl() {
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

                var targetPost = "https://www.ptt.cc" + posts[rand.Next(0, posts.Length)];
                tempPage.LoadHtml(
                    await client.GetStringAsync(targetPost)
                );

                var title = tempPage.DocumentNode.SelectSingleNode("//title").InnerText.Replace(" - 看板 Beauty - 批踢踢實業坊", "");

                if (!title.StartsWith("[正妹]")) {
                    return null;
                }

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
    }
}
