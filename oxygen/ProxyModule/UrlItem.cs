using System;
using System.Collections.Generic;

namespace Proxygen
{
    public class UrlItem
    {
        public UrlItem()
        {
            Messages = new List<string>();
        }

        /// <summary>
        /// for diagnostics
        /// </summary>
        public List<string> Messages { set; get; }

        public UrlItem(string pathAndQueryPrefix = "/", string hostName = "localhost", int port = 80,string scheme = "http")
        {
            HostName = hostName;
            Match = pathAndQueryPrefix;
            Port = port;
            Scheme = scheme;
        }

        public string HostName { set; get; }

        public string Match { set; get; }

        public int? Port { set; get; }

        public string Scheme { set; get; }

        /// <summary>
        /// for diagnostics
        /// </summary>
        public DateTime? Time { set; get; }
    }
}