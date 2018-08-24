using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SimpleLineBot.ReplyModules.GitHubCI.Models {
    public class GitHubPayload {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("ref_type")]
        public string RefType { get; set; }

        [JsonProperty("master_branch")]
        public string MasterBranch { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("pusher_type")]
        public string PusherType { get; set; }

        [JsonProperty("repository")]
        public GitHubRepository Repository { get; set; }

        [JsonProperty("sender")]
        public GitHubUser Sender { get; set; }
    }
}
