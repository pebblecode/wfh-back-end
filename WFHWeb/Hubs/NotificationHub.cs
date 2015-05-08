using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using WFHWeb.Controllers;
using WFHWeb.Models;

namespace WFHWeb.Hubs
{
    [HubName("notification")]
    public class NotificationHub : Hub
    {
        public static void NotifyUsers(IList<UserStatusInfo> userStatuses)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            hub.Clients.All.update(userStatuses);
        }

        public async override Task OnConnected()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://wfh.azurewebsites.net/api/");
            var result = await httpClient.GetAsync("statuses");
            var resultString = await result.Content.ReadAsStringAsync();
            var blob = JsonConvert.DeserializeObject<List<UserStatusInfo>>(resultString);

            this.Clients.Caller.update(blob);
        }
    }
}