using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyModule
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
            var mappingUsed = new ProxyRules(ProxyConfig.Data.Map).Transform(request);
            if (mappingUsed == null)
            {
                return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
            }
            var client = new HttpClient();
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response;
        }
    }

    public class InternalHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var client = new HttpClient();
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response;
        }
    }
}