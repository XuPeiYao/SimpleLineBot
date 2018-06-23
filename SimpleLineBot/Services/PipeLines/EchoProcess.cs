using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Line;

namespace SimpleLineBot.Services.PipeLines {
    public class EchoProcess : IPipeLineProcess {
        public ILineBot Bot { get; set; }
        public EchoProcess(ILineBot bot) {
            Bot = bot;
        }
        public async Task<bool> Handle(ILineEvent e) {
            if (e.EventType != LineEventType.Message) return false;

            if (e.Message?.Text?.IndexOf("echo:") != 0) return false;

            var context = e.Message.Text.Split(':').Last();

            await Bot.Reply(e.ReplyToken, new TextMessage($"收到指令:{context}"));

            return true;
        }
    }
}
