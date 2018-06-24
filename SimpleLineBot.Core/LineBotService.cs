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
        public static List<Type> ReplyProcesses { get; set; }
        public ILineBot Bot { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public LineBotService(ILineBot lineBot, IServiceProvider serviceProvider) {
            Bot = lineBot;
            ServiceProvider = serviceProvider;
        }

        public async Task EventHandle(ILineEvent e) {
            foreach (var process in ReplyProcesses) {
                var pipe = ServiceProvider.GetService(process) as ILineReplyProcess;

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
            return context.RequestServices.GetService<LineBotService>().Handle(context);
        }
    }
}
