using System.Web.Http.Filters;
using System.Net.Http;
using System.Net;

namespace QA.API.Filter
{   
    /// <summary>
    /// 认证appid和aid
    /// </summary>
    public class BaseAuth : ActionFilterAttribute
    {
        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            bool vaildedtoken = true;
            //
            try
            {
                var request = actionContext.ActionArguments;
                string appid = request["appid"].ToString();
                string aid = request["aid"].ToString();
                string q = request["q"].ToString();

                if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(aid)
                    || string.IsNullOrEmpty(q))
                {
                    vaildedtoken = false;
                }

                vaildedtoken = Tool.ValiAppID.IsValid(appid, aid, q);

            }
            catch { vaildedtoken = false; }

            if (vaildedtoken == false)
            {
                var response = new HttpResponseMessage();
                response.Content = new StringContent("参数不正确");
                response.StatusCode = HttpStatusCode.BadRequest;
                actionContext.Response = response;

            }           
            base.OnActionExecuting(actionContext);
        }        
        
    }   
}