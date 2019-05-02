using HtmlAgilityPack;
using Line;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleLineBot.ReplyModules.PttBeauty {
    public class PttBeautyProcess : ILineReplyModule {
        public ILineBot Bot { get; set; }

        public Uri PostUrl { get; set; }
        public string Title { get; set; }

        public static IDatabase redis { get; set; }
        public static IServer redisServer { get; set; }
        public PttBeautyProcess(ILineBot bot) {
            if (redis == null) {
                var redisConnection = ConnectionMultiplexer.Connect("192.168.1.2");
                redis = redisConnection.GetDatabase();
                redisServer = redisConnection.GetServer(IPAddress.Parse("192.168.1.2"), 6379);
            }

            Bot = bot;
        }

        public async Task<bool> Handle(ILineEvent e) {
            if (e.EventType != LineEventType.Message) return false;

            Regex command = new Regex(@"\S*妹子!?$");

            if (!command.IsMatch(e.Message?.Text ?? "")) return false;

            var keyword = e.Message.Text.Substring(0, e.Message.Text.IndexOf("妹子"));

            HttpClient client = new HttpClient();
            Uri image = null;
            int tryLimit = 3;

            if (keyword.Length == 0) {
                while (image == null && tryLimit > 0) {
                    var url = await GetImageUrl();
                    if (url != null) {
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK) {
                            image = new Uri(url);
                        }
                    }
                    tryLimit--;
                }
            } else {
                while (image == null && tryLimit > 0) {
                    var url = await GetImageUrlByKeyword(keyword);
                    if (url != null) {
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK) {
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
                    });
            } else {
                ISendMessage message;
                if (e.Message.Text.Contains("!")) {
                    message = new ImageMessage() {
                        Url = image,
                        PreviewUrl = image
                    };
                } else {
                    message = new TemplateMessage() {
                        Template = new ButtonsTemplate() {
                            ImageSize = ImageSize.Contain,
                            ImageAspectRatio = ImageAspectRatio.Square,
                            Title = this.Title,
                            Text = this.Title,
                            ThumbnailUrl = image,
                            Actions = new IAction[] {
                                new MessageAction() {
                                    Label ="再來一個妹子",
                                    Text = e.Message.Text
                                }
                            }
                        },
                        AlternativeText = "您的裝置不支援顯示此內容，請嘗試使用智慧型手機觀看或在指令後加入驚嘆號!"
                    };

                }
                await Bot.Reply(e.ReplyToken, message);
            }
            return true;
        }

        public async Task<string> GetImageUrl() {
            var key = redis.KeyRandom(CommandFlags.NoScriptCache);

            var length = await redis.ListLengthAsync(key);

            Random rnd = new Random((int)DateTime.Now.Ticks);
            int index = rnd.Next((int)length);

            return await redis.ListGetByIndexAsync(key, index);
        }

        public async Task<string> GetImageUrlByKeyword(string keyword) {
            var keys = redisServer.Keys(0, $"*{keyword.Replace("的", "")}*", 1).ToList();

            if (keys.Count == 0) {
                return null;
            }

            var key = keys.First();
            var length = await redis.ListLengthAsync(key);

            Random rnd = new Random((int)DateTime.Now.Ticks);
            int index = rnd.Next((int)length);

            return await redis.ListGetByIndexAsync(key, index);
        }
    }
}
