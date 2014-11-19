using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ProxyModule
{
    public class ProxyRules
    {
        public ProxyRules(Dictionary<string, UrlItemMapping> map)
        {
            Map = map;
        }

        public ProxyRules()
        {
           
        }

        public Dictionary<string, UrlItemMapping> Map { set; get; }

        public UrlItemMapping Transform(HttpRequestMessage request)
        {
            UrlItemMapping[] urlIdentityMap = { null };
            bool[] isMatched = {false};
            foreach (
                var urlIdentityMapping in
                    Map.Where(
                        urlIdentityMapping =>
                         !isMatched[0] &&
                            urlIdentityMapping.Value.From.HostName == request.RequestUri.Host &&
                            urlIdentityMapping.Value.From.Port == request.RequestUri.Port &&
                            urlIdentityMapping.Value.From.Scheme == request.RequestUri.Scheme )
                )
            {
                var rgx = new Regex(urlIdentityMapping.Value.From.Match);
                if (!rgx.IsMatch(request.RequestUri.PathAndQuery)) continue;
                isMatched[0] = true;
                urlIdentityMap[0] = urlIdentityMapping.Value;
            }

            if (isMatched[0])
            {
                 if (request.Method == HttpMethod.Get)
                request.Content = null;

            var forwardUri = new UriBuilder(request.RequestUri)
            {
                Host = urlIdentityMap[0].To.HostName,
                Port = urlIdentityMap[0].To.Port,
                Scheme = urlIdentityMap[0].To.Scheme,
                Path = urlIdentityMap[0].To.Match + request.RequestUri.PathAndQuery

            };


            //strip off the proxy port and replace with an Http port
            //send it on to the requested URL
            request.RequestUri = forwardUri.Uri;

            return urlIdentityMap[0];
            }

            return null;


        }
    }
}