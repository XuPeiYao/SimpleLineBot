using Line;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleLineBot.ReplyModules.Common {
    public class ModuleManager : ILineReplyModule {
        public ILineBot Bot { get; set; }
        public LineBotService Service { get; set; }
        public ILineConfiguration Configuration { get; set; }
        public static List<string> SuperUsers { get; set; } = new List<string>();
        public ModuleManager(
            ILineConfiguration configuration,
            ILineBot bot,
            LineBotService service) {
            Configuration = configuration;
            Bot = bot;
            Service = service;
        }
        public async Task<bool> Handle(ILineEvent e) {
            if (e.EventType != LineEventType.Message) return false;

            string message = e.Message?.Text?.Trim();
            if (message == null || !message.StartsWith("linebot ")) {
                return false;
            }

            var parameters = message.Split(' ').Skip(1).ToArray();

            var targetMethod = GetType().GetMethods().FirstOrDefault(x => x.Name.ToLower() == parameters[0]);


            if (targetMethod == null || targetMethod.Name == "Handle") {
                await Bot.Reply(e.ReplyToken, new TextMessage($"無效的指令"));
                return true;
            }

            return await ((Task<bool>)targetMethod.Invoke(this, new object[] { e }.Concat(parameters.Skip(1)).ToArray()));
        }

        public async Task<bool> Help(ILineEvent e) {
            await Bot.Reply(e.ReplyToken, new TextMessage(
                $"您可以執行以下命令: install uninstall enable disable list login logout stop"
            ));
            return true;
        }

        public async Task<bool> Install(ILineEvent e, string moduleName, string moduleZipPath) {
            if (!SuperUsers.Contains(e.Source.User.Id)) {
                await Bot.Reply(e.ReplyToken, new TextMessage($"無權限執行此命令"));
                return true;
            }

            var client = new HttpClient();
            var temp = Path.GetTempFileName();

            using (var fileStream = File.Create(temp)) {
                (await client.GetStreamAsync(moduleZipPath)).CopyTo(fileStream);
            }

            using (var fileStream = File.Open(temp, FileMode.Open))
            using (ZipArchive arch = new ZipArchive(fileStream, ZipArchiveMode.Read, true)) {
                arch.ExtractToDirectory(Path.Combine(LineBotService.ContentRootPath, "plugins", moduleName));
            }

            File.Delete(temp);

            await Bot.Reply(e.ReplyToken, new TextMessage($"已安裝，請重啟服務"));

            return true;
        }

        public async Task<bool> Uninstall(ILineEvent e, string moduleName) {
            if (!SuperUsers.Contains(e.Source.User.Id)) {
                await Bot.Reply(e.ReplyToken, new TextMessage($"無權限執行此命令"));
                return true;
            }

            try {
                Directory.Delete(Path.Combine(LineBotService.ContentRootPath, "plugins", moduleName), true);
                await Bot.Reply(e.ReplyToken, new TextMessage($"已解除安裝，請重啟服務"));
            } catch {
                await Bot.Reply(e.ReplyToken, new TextMessage($"解除安裝失敗，請關閉服務後手動刪除目錄"));
            }
            return true;
        }

        public async Task<bool> Enable(ILineEvent e, string moduleName) {
            if (!SuperUsers.Contains(e.Source.User.Id)) {
                await Bot.Reply(e.ReplyToken, new TextMessage($"無權限執行此命令"));
                return true;
            }

            var moduleIndex = LineBotService.ReplyModules.FindIndex(x => x.type.Name.ToLower() == moduleName.ToLower());
            LineBotService.ReplyModules[moduleIndex] = (LineBotService.ReplyModules[moduleIndex].type, true);

            await Bot.Reply(e.ReplyToken, new TextMessage($"成功啟用模組: {moduleName}"));

            string[] moduleNames = new string[0];
            using (var file = File.Open("disableModules.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) {
                moduleNames = new StreamReader(file).ReadToEnd()?.Split(',') ?? new string[0];
                moduleNames = moduleNames.Where(x => x.Trim() != moduleName.Trim()).Distinct().ToArray();
            }

            File.Delete("disableModules.txt");

            File.WriteAllText("disableModules.txt", string.Join(",", moduleNames));

            return true;
        }

        public async Task<bool> Disable(ILineEvent e, string moduleName) {
            if (!SuperUsers.Contains(e.Source.User.Id)) {
                await Bot.Reply(e.ReplyToken, new TextMessage($"無權限執行此命令"));
                return true;
            }

            var moduleIndex = LineBotService.ReplyModules.FindIndex(x => x.type.Name.ToLower() == moduleName.ToLower());
            LineBotService.ReplyModules[moduleIndex] = (LineBotService.ReplyModules[moduleIndex].type, false);

            await Bot.Reply(e.ReplyToken, new TextMessage($"成功停用模組: {moduleName}"));

            string[] moduleNames = new string[0];
            using (var file = File.Open("disableModules.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) {
                moduleNames = new StreamReader(file).ReadToEnd()?.Split(',') ?? new string[0];
                moduleNames = moduleNames.Concat(new string[] { moduleName }).Select(x => x.Trim()).Distinct().ToArray();
            }

            File.Delete("disableModules.txt");

            File.WriteAllText("disableModules.txt", string.Join(",", moduleNames));
            return true;
        }

        public async Task<bool> List(ILineEvent e) {
            if (!SuperUsers.Contains(e.Source.User.Id)) {
                await Bot.Reply(e.ReplyToken, new TextMessage($"無權限執行此命令"));
                return true;
            }

            var modulesList = string.Join("\r\n", LineBotService.ReplyModules.Select(x => x.type.Name + (x.enable ? "" : "(停用)")));

            await Bot.Reply(e.ReplyToken, new TextMessage($"目前已經安裝以下模組:\r\n{modulesList}"));

            return true;
        }

        public async Task<bool> Logout(ILineEvent e) {
            if (SuperUsers.Contains(e.Source.User.Id)) {
                SuperUsers.Remove(e.Source.User.Id);
            }

            var userProfile = await Bot.GetProfile(e.Source.User);

            await Bot.Reply(e.ReplyToken, new TextMessage($"{userProfile.DisplayName} 已經登出為一般使用者"));

            return true;
        }

        public async Task<bool> Login(ILineEvent e, string channelSecret) {
            if (channelSecret != Configuration.ChannelSecret) {
                await Bot.Reply(e.ReplyToken, new TextMessage($"登入失敗，ChannelSecret錯誤"));
                return true;
            }

            SuperUsers.Add(e.Source.User.Id);

            var userProfile = await Bot.GetProfile(e.Source.User);

            await Bot.Reply(e.ReplyToken, new TextMessage($"{userProfile.DisplayName} 已經登入為超級使用者"));

            return true;
        }

        public async Task<bool> Stop(ILineEvent e) {
            if (!SuperUsers.Contains(e.Source.User.Id)) {
                await Bot.Reply(e.ReplyToken, new TextMessage($"無權限執行此命令"));
                return true;
            }

            await Bot.Reply(e.ReplyToken, new TextMessage($"正在關閉聊天機器人服務"));

            Environment.Exit(0);

            return true;
        }
    }
}
