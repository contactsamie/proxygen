using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;

namespace Proxygen
{
    // Install-Package Microsoft.AspNet.WebApi
    /// <summary>
    ///     the proxy to listen for new requests while previous
    ///     requests are still pending responses, perfect for a proxy.
    /// </summary>
    public class ProxyHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();
            var mappingUsed = new UrlItemMapping();
            var problematicConfigFile = true;
            try
            {
                Dictionary<string, UrlItemMapping> map = ProxyConfig.Data.Map;
                problematicConfigFile = false;

                mappingUsed = new ProxyRules(map).Transform(request);
                if (mappingUsed == null) throw new ServerException("error in mapping code");
                if (mappingUsed.From == null) throw new ServerException("error in mapping code - From");
                if (mappingUsed.To == null)
                {
                    mappingUsed.From.Messages = mappingUsed.From.Messages ?? new List<string>();
                    mappingUsed.From.Messages.Add("No Matching Route Was Found");
                    response.StatusCode = HttpStatusCode.NotFound;
                }
                else
                {
                    response = await ProcessMatchedRoute(request, cancellationToken, mappingUsed, response);
                }
            }
            catch (Exception e)
            {
                mappingUsed = mappingUsed ?? new UrlItemMapping();
                mappingUsed.From = mappingUsed.From ?? new UrlItemFrom();
                mappingUsed.From.Messages = mappingUsed.From.Messages ?? new List<string>();
                if (problematicConfigFile)
                {
                    mappingUsed.From.Messages.Add("There is an error in system configuration file : " + e.Message);

                    mappingUsed.From.HostName = request.RequestUri.Host;
                    mappingUsed.From.Port = request.RequestUri.Port;
                    mappingUsed.From.Scheme = request.RequestUri.Scheme;

                    mappingUsed.From.LogDestination = "";
                }
                else
                {
                    mappingUsed.From.Messages.Add("Exception : " + e.Message);
                }

                mappingUsed.From.Messages.Add("Inner Exception : " + e.InnerException);
                mappingUsed.From.Messages.Add("Exception StackTrace: " + e.StackTrace);

                response = new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            response = FinalizeResponse(mappingUsed, response);

            return response;
        }

        private static void SeedConfig()
        {
            ProxyConfig.Config.Advanced.AllowOverwrite = true;
            ProxyConfig.Config.Advanced.PersistedData = new ProxyRules()
            {
                Map = new Dictionary<string, UrlItemMapping>
            {
                {
                    "localhost",
                    new UrlItemMapping
                    {
                        From = new UrlItemFrom
                        {
                            HostName = "",
                            Match = "",
                            Port = 0,
                            Messages = new List<string>(),
                            Scheme = "",
                            LogDestination = "",
                            MustContain = "",
                            MustRemove = "",
                            OverrideReturnWithSystemMessages = true,
                            RequestID = null
                        },
                        To = new UrlItemTo
                        {
                            HostName = "",
                            Match = "",
                            Port = 0,
                            RespondWithCode = 123,
                            IsSuccessful = true,
                            Messages = new List<string>(),
                            Return = "",
                            Scheme = ""
                        }
                    }
                }
            }
            };

            ProxyConfig.Config.Advanced.AllowOverwrite = false;
        }

        private static HttpResponseMessage FinalizeResponse(UrlItemMapping mappingUsed, HttpResponseMessage response)
        {
            mappingUsed.From.RequestID = mappingUsed.From.RequestID ?? Guid.NewGuid();
            mappingUsed.From.Time = DateTime.Now;

            if (mappingUsed.From.LogDestination != null)
            {
                string loggable = Environment.NewLine +
                                  "=================================================== PROXYGEN LOG (ID: " +
                                  mappingUsed.From.RequestID + ") >" + DateTime.Now +
                                  "===============================================" +
                                  Environment.NewLine +
                                  JsonConvert.SerializeObject(mappingUsed, Formatting.Indented) +
                                  Environment.NewLine;
                var alternateLogPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                                       "\\PROXYGEN-LOG.txt";
                try
                {
                    File.AppendAllText(
                        string.IsNullOrEmpty(mappingUsed.From.LogDestination)
                            ? alternateLogPath
                            : mappingUsed.From.LogDestination, loggable);
                }
                catch (Exception e)
                {
                    mappingUsed.From.Messages.Add("Error while trying to log : " + e.Message);
                    File.AppendAllText(alternateLogPath, loggable);
                    response.StatusCode = HttpStatusCode.NotFound;
                }
            }

            if (mappingUsed.From.OverrideReturnWithSystemMessages)
            {
                response.Content = new JsonContent(JObject.Parse(JsonConvert.SerializeObject(mappingUsed)));
            }
            return response;
        }

        private static async Task<HttpResponseMessage> ProcessMatchedRoute(HttpRequestMessage request,
            CancellationToken cancellationToken,
            UrlItemMapping mappingUsed, HttpResponseMessage response)
        {
            mappingUsed.From.Messages = mappingUsed.From.Messages ?? new List<string>();
            if (mappingUsed.To.Return == null)
            {
                mappingUsed.From.Messages.Add("Trying to connect to remote server at " + request.RequestUri.Host + " with query: " + request.RequestUri.PathAndQuery);
                response =
                    await
                        new HttpClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                mappingUsed.From.Messages.Add("ResponseHeadersRead! Got a " + response.StatusCode);
            }
            else
            {
                mappingUsed.From.Messages.Add("Trying to create a mock response from supplied data ....");
                response.Content = new JsonContent(JObject.Parse(mappingUsed.To.Return));
                mappingUsed.From.Messages.Add("Mock response created successfully");
            }

            if (mappingUsed.To.RespondWithCode != null)
            {
                HttpStatusCode resultCode;

                mappingUsed.From.Messages.Add("Substiting Status Code " + response.StatusCode + " with " + mappingUsed.To.RespondWithCode);

                if (Enum.TryParse(mappingUsed.To.RespondWithCode.ToString(), out resultCode))
                {
                    response.StatusCode = resultCode;
                }
                else
                {
                    string err = mappingUsed.To.RespondWithCode +
                                 " provided could not be matched with any standard HttpStatusCode";
                    mappingUsed.To.Messages = mappingUsed.To.Messages ?? new List<string>();
                    mappingUsed.To.Messages.Add(err);
                    throw new ServerException(err);
                }
            }

            mappingUsed.To.IsSuccessful = true;
            mappingUsed.To.Time = DateTime.Now;
            return response;
        }
    }
}