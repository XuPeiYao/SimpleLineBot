using Line;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleLineBot.ReplyModules.Common {
    public class ModuleManager : ILineReplyModule {
        public ILineBot Bot { get; set; }
        public LineBotService Service { get; set; }
        public ModuleManager(ILineBot bot, LineBotService service) {
            Bot = bot;
            Service = service;
        }
        public async Task<bool> Handle(ILineEvent e) {
            if (e.EventType != LineEventType.Message) return false;

            switch (e.Message?.Text?.Trim()) {
                case "linebot list":
                    await Bot.Reply(e.ReplyToken, new TextMessage($"目前已安裝模組:\r\n{string.Join("\r\n", LineBotService.ReplyModules.Select(x => x.type.Name))}"));

                    return true;
                case "linebot help":
                    await Bot.Reply(e.ReplyToken, new TextMessage($"可使用命令:\r\n{string.Join("\r\n", "list help")}"));

                    return true;
            }

            Regex regex = new Regex(@"linebot (enable,disable) .+");

            if (regex.IsMatch(e.Message?.Text ?? "")) {
                var moduleName = e.Message.Text.Split(' ').Last();
                var module = LineBotService.ReplyModules.SingleOrDefault(x => x.type.Name.Equals(moduleName, StringComparison.CurrentCultureIgnoreCase));
                if (module.type == null) {
                    await Bot.Reply(e.ReplyToken, new TextMessage($"找不到模組: {moduleName}"));
                }
                if (e.Message.Text.Contains(" enable ")) {
                    module.enable = true;
                    await Bot.Reply(e.ReplyToken, new TextMessage($"已啟用模組: {moduleName}"));
                } else {
                    module.enable = false;
                    await Bot.Reply(e.ReplyToken, new TextMessage($"已停用模組: {moduleName}"));
                }
                return true;
            }

            return false;
        }
    }
}
