using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SimpleLineBot.ReplyModules.GitHubCI.Models {
    public class GitHubRepository {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("owner")]
        public GitHubUser Owner { get; set; }
    }
}
