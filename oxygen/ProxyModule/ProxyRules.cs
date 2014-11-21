using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Proxygen
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
            UrlItemMapping[] urlIdentityMap = { new UrlItemMapping()
            {
                To = null,
                From = new UrlItemFrom()
                {
                    HostName = request.RequestUri.Host,
                   Port = request.RequestUri.Port,
                   Scheme = request.RequestUri.Scheme
                }
            } };
            bool[] isMatched = { false };
            foreach (
                var urlIdentityMapping in
                    Map.Where(
                        urlIdentityMapping =>
                         !isMatched[0] &&
                            urlIdentityMapping.Value.From.HostName == request.RequestUri.Host &&
                            urlIdentityMapping.Value.From.Port == request.RequestUri.Port &&
                            urlIdentityMapping.Value.From.Scheme == request.RequestUri.Scheme)
                )
            {
                if (new Regex(urlIdentityMapping.Value.From.Match).IsMatch(request.RequestUri.PathAndQuery))
                {
                    if (!string.IsNullOrEmpty(urlIdentityMapping.Value.From.MustContain))
                    {
                        if (!request.RequestUri.PathAndQuery.Contains(urlIdentityMapping.Value.From.MustContain)) continue;
                    }

                    urlIdentityMap[0] = urlIdentityMapping.Value;
                    isMatched[0] = true;
                }
            }
            urlIdentityMap[0].From.Time = DateTime.Now;
            urlIdentityMap[0].From.Messages = urlIdentityMap[0].From.Messages ?? new List<string>();
            urlIdentityMap[0].From.Messages.Add("PathAndQuery:" + request.RequestUri.PathAndQuery);
            urlIdentityMap[0].From.Messages.Add("Method:" + request.Method);
            urlIdentityMap[0].From.Messages.Add("Version:" + request.Version);
            urlIdentityMap[0].From.Messages.Add("Headers:" + request.Headers);
            if (!isMatched[0]) return urlIdentityMap[0];

            if (request.Method == HttpMethod.Get)
                request.Content = null;

            //remove parts before creating new request
            var pathAndQuery = string.IsNullOrEmpty(urlIdentityMap[0].From.MustRemove) ? request.RequestUri.PathAndQuery : request.RequestUri.PathAndQuery.Replace(urlIdentityMap[0].From.MustRemove, "");

            //forward to http port 80 by default
            var forwardUri = new UriBuilder(request.RequestUri)
            {
                Host = urlIdentityMap[0].To.HostName,
                Port = urlIdentityMap[0].To.Port ?? 80,
                Scheme = urlIdentityMap[0].To.Scheme,
                Path = urlIdentityMap[0].To.Match + pathAndQuery.Split('?')[0]
            };

            //strip off the proxy port and replace with an Http port
            //send it on to the requested URL
            request.RequestUri = forwardUri.Uri;

            return urlIdentityMap[0];
        }
    }
}