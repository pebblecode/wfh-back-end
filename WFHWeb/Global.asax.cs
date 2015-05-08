using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace WFHWeb
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
