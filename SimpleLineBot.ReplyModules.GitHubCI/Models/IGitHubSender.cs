using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLineBot.ReplyModules.GitHubCI.Models {
    public interface IGitHubSender {
        string Type { get; }
    }
}
