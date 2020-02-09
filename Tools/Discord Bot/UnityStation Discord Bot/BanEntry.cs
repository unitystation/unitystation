using System;
using System.Collections.Generic;
using System.Text;

namespace UnityStation_Discord_Bot
{
    public class BanEntry
    {
        public string userId { get; set; }
        public string userName { get; set; }
        public double minutes { get; set; }
        public string dateTimeOfBan { get; set; }
        public string reason { get; set; }
    }
}
