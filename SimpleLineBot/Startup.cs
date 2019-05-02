using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Line;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleLineBot.Core;
using SimpleLineBot.ReplyModules.Common;
using SimpleLineBot.ReplyModules.GitHubCI.Controllers;
using SimpleLineBot.ReplyModules.PttBeauty;

namespace SimpleLineBot {
    public class Startup {
        public Startup(IConfiguration configuration, IHostingEnvironment env) {
            Configuration = configuration;
            Environment = env;
        }

        /// <summary>
        /// 設定
        /// </summary>
        public static IConfiguration Configuration { get; set; }
        public static IHostingEnvironment Environment { get; set; }
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddSingleton(typeof(ILineConfiguration), (IServiceProvider serviceProvider) => {
                var instance = new LineConfiguration(); //建立Line機器人實例
                Configuration.Bind("LineConfiguration", instance); //綁定屬性
                return instance;
            });
            services.AddSingleton(typeof(ILineBot), typeof(LineBot));
            services.AddScoped(typeof(LineBotService));
            //services.InstallModules(Environment.ContentRootPath);

            LineBotService.ReplyModules = new List<(Type type, bool enable)>();

            LineBotService.ReplyModules.Add((typeof(EchoModule), true));
            services.AddScoped(typeof(EchoModule));
            LineBotService.ReplyModules.Add((typeof(PttBeautyProcess), true));
            services.AddScoped(typeof(PttBeautyProcess));

            services.AddMvc().AddApplicationPart(typeof(CiController).Assembly);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.Use(async (context, next) => {
                Console.WriteLine("HasRequest");

                await next();
            });


            app.UseMvc();

            app.Run(LineBotService.Run);
        }
    }
}
