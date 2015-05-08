using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.SQLite;

namespace WFHWeb.Controllers
{
    [RoutePrefix("/api/statuses")]
    public class StatusesController : ApiController
    {
        [HttpPost]
        [Route("{status}")]
        public void SetStatus()
        {
            
        }
    }
}
