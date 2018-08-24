using Line;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace SimpleLineBot {
    public class LineBotService {
        public static List<(Type type, bool enable)> ReplyModules { get; set; }
        public ILineBot Bot { get; set; }
        private IServiceProvider ServiceProvider { get; set; }
        public static string ContentRootPath { get; set; }

        public LineBotService(ILineBot lineBot, IServiceProvider serviceProvider) {
            Bot = lineBot;
            ServiceProvider = serviceProvider;
        }

        public async Task EventHandle(ILineEvent e) {
            Console.WriteLine($"Request: {e.EventType}");

            foreach (var module in ReplyModules) {
                if (!module.enable) continue;

                var pipe = ServiceProvider.GetService(module.type) as ILineReplyModule;

                if (await pipe.Handle(e)) break;
            }
        }

        public async Task Handle(HttpContext context) {
            if (context.Request.Method != HttpMethods.Post) {
                context.Response.StatusCode = 404;
                return;
            }

            var events = await Bot.GetEvents(context.Request);

            Parallel.ForEach(events, e => EventHandle(e).GetAwaiter().GetResult());

            context.Response.StatusCode = 200;
        }

        /// <summary>
        /// DI進入點
        /// </summary>
        public static Task Run(HttpContext context) {
            Console.WriteLine($"Request {DateTime.Now}");

            return context.RequestServices.GetService<LineBotService>().Handle(context);
        }
    }
}
