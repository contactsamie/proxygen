using System.Web.Http;

namespace Proxygen
{
    public class ProxyApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}