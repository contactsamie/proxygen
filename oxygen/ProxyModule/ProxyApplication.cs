using System.Web.Http;

namespace ProxyModule
{
    public class ProxyApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}