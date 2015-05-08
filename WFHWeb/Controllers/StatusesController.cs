using System.Threading.Tasks;
using System.Web.Http;

namespace WFHWeb.Controllers
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Web;
    using WFHWeb.DataModels;
    using WFHWeb.Models;
    using WFHWeb.Services;

    [RoutePrefix("api/statuses")]
    public class StatusesController : ApiController
    {
        private readonly string dataDir;

        public StatusesController()
        {
            this.dataDir = HttpContext.Current.Server.MapPath("~/App_Data");
        }

        [HttpPost]
        [Route("{statusType}")]
        public IHttpActionResult SetStatus([FromUri]StatusType statusType, [FromBody]UserStatus userStatus)
        {
            var workingStatusData = new WorkingStatusData
            {
                StatusType = statusType, 
                Email = userStatus.Email,
                StatusDetails = userStatus.StatusDetails
            };
            StatusService.Instance.SetStatus(this.dataDir, workingStatusData);

            return this.Ok();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetStatuses()
        {
            IList<WorkingStatusData> currentWorkingStatuses = StatusService.Instance.GetAllStatuses(this.dataDir);
            IList<WorkingStatusData> defaultWorkingStatuses = StatusService.Instance.GetAllStatuses(this.dataDir, true);

            return this.Ok(ToUserStatusInfo(currentWorkingStatuses, defaultWorkingStatuses));
        }

        private static List<UserStatusInfo> ToUserStatusInfo(IList<WorkingStatusData> currentWorkingStatuses, IEnumerable<WorkingStatusData> defaultWorkingStatuses)
        {
            var updatedUsers = currentWorkingStatuses.Select(ws => ws.Email).ToList();
            List<UserStatusInfo> userStatusInfos = currentWorkingStatuses.Select(ws => ToUserStatusInfo(ws, false)).ToList();
            userStatusInfos.AddRange(defaultWorkingStatuses.Where(ws => !updatedUsers.Contains(ws.Email)).Select(ws => ToUserStatusInfo(ws, true)));

            return userStatusInfos;
        }

        private static UserStatusInfo ToUserStatusInfo(WorkingStatusData workingStatusData, bool isDefault)
        {
            return new UserStatusInfo
            {
                Email = workingStatusData.Email,
                Status = new StatusInfo
                {
                    StatusType = workingStatusData.StatusType,
                    StatusDetails = workingStatusData.StatusDetails,
                    InOffice = workingStatusData.StatusType == StatusType.WorkInOffice,
                    Default = isDefault
                }
            };
        }

        [HttpPost]
        [Route("Slack")]
        public async Task<IHttpActionResult> SetStatusFromSlack()
        {
            // this is simon's token :)
            var token = "xoxp-2165237499-2607133545-4790338792-f5ac10";
            var foo = await this.Request.Content.ReadAsStringAsync();
            var slackData = HttpUtility.ParseQueryString(foo);
            var userid = slackData["user_id"];

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://slack.com/api/");
            var response = await httpClient.GetAsync(string.Format("users.info?token={0}&&user={1}", token, userid));
            var jsonBlob = JObject.Parse(await response.Content.ReadAsStringAsync());
            var email = (string)jsonBlob["user"]["profile"]["email"];
            
            this.SetStatus(StatusType.WorkOutOfOffice, new UserStatus { Email = email });

            return Ok("Yo dawg, you're working from home");
        }
    }
}
