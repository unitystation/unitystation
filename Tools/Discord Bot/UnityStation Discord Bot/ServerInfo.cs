using System;
using System.Collections.Generic;
using System.Text;

namespace UnityStation_Discord_Bot
{
    public class ServerInfo
    {
        public string ServerName { get; set; }
        public string ForkName { get; set; }
        public int BuildVersion { get; set; }

        public uint PlayerCount { get; set; }

    }
}
