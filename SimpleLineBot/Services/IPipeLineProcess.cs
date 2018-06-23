using Line;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleLineBot.Services {
    public interface IPipeLineProcess {
        Task<bool> Handle(ILineEvent e);
    }
}
