using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WFHWeb.Models;

namespace WFHWeb.Hubs
{
    public class NotificationHub : Hub
    {
        public static void NotifyUsers(IList<UserStatusInfo> userStatuses)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            hub.Clients.All.update(userStatuses);
        }
    }
}