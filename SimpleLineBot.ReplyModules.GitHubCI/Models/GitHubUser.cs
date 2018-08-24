using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SimpleLineBot.ReplyModules.GitHubCI.Models {
    public class GitHubUser : IGitHubSender {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
