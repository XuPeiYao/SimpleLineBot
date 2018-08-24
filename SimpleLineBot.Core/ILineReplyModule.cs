using Line;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleLineBot {
    /// <summary>
    /// LINE機器人回應程序
    /// </summary>
    public interface ILineReplyModule {
        /// <summary>
        /// 處理回應
        /// </summary>
        /// <param name="e">事件內容</param>
        /// <returns>是否攔截，如為<see cref="true"/>則表示不繼續傳遞給其他處理程序</returns>
        Task<bool> Handle(ILineEvent e);
    }
}
