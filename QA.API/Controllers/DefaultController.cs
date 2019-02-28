using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace QA.API.Controllers
{
    [Authorize]
   // [RoutePrefix("api/Query")]
    [EnableCors("*", "*", "*")]
    public class DefaultController : ApiController
    {
        // GET: api/Default
        [Route("myget")]
        public object Get()
        {
            return new { a="value1", b="value2" };
        }

        // GET: api/Default/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Default
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/Default/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Default/5
        public void Delete(int id)
        {
        }
    }
}
