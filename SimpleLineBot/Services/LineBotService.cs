using Line;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using SimpleLineBot.Services.PipeLines;

namespace SimpleLineBot.Services {
    /// <summary>
    /// 事件處理程序
    /// </summary>
    /// <param name="e">事件</param>
    /// <param name="handled">是否攔截處理</param>
    public delegate void EventHandleProcess();

    public class LineBotService {
        public static List<Type> PipeLine { get; set; }
        public ILineBot Bot { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public LineBotService(ILineBot lineBot, IServiceProvider serviceProvider) {
            Bot = lineBot;
            ServiceProvider = serviceProvider;
        }

        public async Task EventHandle(ILineEvent e) {
            foreach (var process in PipeLine) {
                var pipe = ServiceProvider.GetService(process) as IPipeLineProcess;

                if (await pipe.Handle(e)) break;
            }
        }

        public async Task Handle(HttpContext context) {
            if (context.Request.Method != HttpMethods.Post) return;

            var events = await Bot.GetEvents(context.Request);

            Parallel.ForEach(events, e => EventHandle(e).GetAwaiter().GetResult());

            context.Response.StatusCode = 200;
        }

        /// <summary>
        /// DI進入點
        /// </summary>
        public static Task Run(HttpContext context) {
            Console.WriteLine("收到訊息!!");
            return context.RequestServices.GetService<LineBotService>().Handle(context);
        }

        public static void InstallPipeLine(IServiceCollection services, params Type[] types) {
            PipeLine = new List<Type>();
            foreach (var type in types) {
                PipeLine.Add(type);
                services.AddScoped(type);
            }
        }
    }
}
