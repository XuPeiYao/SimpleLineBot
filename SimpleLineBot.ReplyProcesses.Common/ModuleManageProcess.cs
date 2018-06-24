using Line;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLineBot.ReplyProcesses.Common {
    public class ModuleManageProcess : ILineReplyProcess {
        public ILineBot Bot { get; set; }
        public LineBotService Service { get; set; }
        public ModuleManageProcess(ILineBot bot, LineBotService service) {
            Bot = bot;
            Service = service;
        }
        public async Task<bool> Handle(ILineEvent e) {
            if (e.EventType != LineEventType.Message) return false;

            switch (e.Message?.Text?.Trim()) {
                case "linebot list":
                    await Bot.Reply(e.ReplyToken, new TextMessage($"目前已安裝模組:\r\n{string.Join("\r\n", LineBotService.ReplyProcesses.Select(x => x.Name))}"));

                    return true;
                case "linebot help":
                    await Bot.Reply(e.ReplyToken, new TextMessage($"可使用命令:\r\n{string.Join("\r\n", "list help")}"));

                    return true;
            }

            return false;
        }
    }
}
