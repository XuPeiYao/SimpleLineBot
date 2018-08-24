using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Line;
using Microsoft.AspNetCore.Mvc;
using SimpleLineBot.ReplyModules.GitHubCI.Models;

namespace SimpleLineBot.ReplyModules.GitHubCI.Controllers {
    [Route("api/[controller]")]
    [Produces(contentType: "application/json")]
    public class CiController : Controller {
        public static ILineBot Bot { get; set; }
        public CiController(ILineBot bot) {
            Bot = bot;
        }

        [HttpGet]
        public async Task<string> Test() {
            return "OK";
        }

        /// <summary>
        /// 讓github webhook呼叫
        /// </summary>
        [HttpPost]
        public async Task Post(
            [FromBody]GitHubPayload payload,
            [FromQuery]string lineId,
            [FromQuery]string jenkinsUrl) {
            var message = new TextMessage() {
                Text = $"專案[{payload.Repository.Name}]，由使用者[{payload.Sender.Login}]觸發建置({payload.Ref})"
            };
            await Bot.Push(lineId, message);

            HttpClient client = new HttpClient();
            try {
                var response = await client.GetAsync(jenkinsUrl);

                if (response.IsSuccessStatusCode) {
                    var success = new TextMessage() {
                        Text = $"專案[{payload.Repository.Name}]({payload.Ref})引動Jenkins成功!正在建置..."
                    };
                    await Bot.Push(lineId, success);
                } else {
                    var error = new TextMessage() {
                        Text = $"專案[{payload.Repository.Name}]({payload.Ref})引動Jenkins失敗!"
                    };
                    await Bot.Push(lineId, error);
                }
            } catch (Exception e) {
                var error = new TextMessage() {
                    Text = $"專案[{payload.Repository.Name}]({payload.Ref})引動Jenkins失敗!"
                };
                var errorData = new TextMessage() {
                    Text = $"錯誤訊息: {e.Message}"
                };
                await Bot.Push(lineId, error, errorData);
            }
        }

        [HttpGet("success")]
        public async Task Success(
            [FromQuery]string lineId,
            [FromQuery]string message) {
            var tmessage = new TextMessage() {
                Text = message
            };
            await Bot.Push(lineId, tmessage);
        }
    }
}