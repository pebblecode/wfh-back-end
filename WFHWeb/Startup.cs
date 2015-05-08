using Microsoft.AspNet.SignalR;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WFHWeb
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var signalrConfig = new HubConfiguration();

            app.MapSignalR(signalrConfig);
        }
    }
}