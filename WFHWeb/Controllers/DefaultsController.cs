using System.Web;

namespace WFHWeb.Controllers
{
    using System.Web.Http;

    using WFHWeb.DataModels;
    using WFHWeb.Models;
    using WFHWeb.Services;

    [RoutePrefix("api/defaults")]
    public class DefaultsController : ApiController
    {
        private readonly string dataDir;
        private IStatusService statusService;

        public DefaultsController()
        {
            this.dataDir = HttpContext.Current.Server.MapPath("~/App_Data");
            this.statusService = new FoldereBasedStatusService();
        }

        [HttpPost]
        [Route("{statusType}")]
        public IHttpActionResult SetStatusDefault([FromUri] StatusType statusType, [FromBody] UserStatus userStatus)
        {
            var workingStatusData = new WorkingStatusData
            {
                StatusType = statusType,
                Email = userStatus.Email,
                StatusDetails = userStatus.StatusDetails
            };
            statusService.SetDefault(this.dataDir, workingStatusData);

            return this.Ok();
        }
    }
}