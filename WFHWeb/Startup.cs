using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WFHWeb;

[assembly: OwinStartup(typeof(Startup))]
namespace WFHWeb
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var signalrConfig = new HubConfiguration();

            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR(signalrConfig);
        }
    }
}