using System.Web.Http;
using System.Web.Http.Cors;

namespace WFHWeb
{
    using WFHWeb.App_Start;

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configure(GlobalConfig.ConfigJson);
            var cors = new EnableCorsAttribute("*", "*", "*");
            GlobalConfiguration.Configuration.EnableCors(cors);
        }
    }
}
