using System.Net.Http;
using System.Web.Http;

namespace Proxygen
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            //config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("Proxy", "{*path}",
                handler: HttpClientFactory.CreatePipeline(new HttpClientHandler(),
                // will never get here if proxy is doing its job
                    new DelegatingHandler[] { new ProxyHandler() }),
                defaults: new
                {
                    path = RouteParameter.Optional
                },
                constraints: null);
        }
    }
}