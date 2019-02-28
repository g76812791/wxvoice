using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;

namespace QA.API.OAuth
{
    public class OpenAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        /// <summary>
        /// 验证客户端
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            string clientId="";
            string clientSecret="";
            if (context.Parameters["grant_type"] == "client_credentials")
            {
                context.TryGetFormCredentials(out clientId, out clientSecret);
                if (!GetUser(clientId, clientSecret))
                {
                    context.SetError("invalid_client", "client is not valid");
                    return;
                }
            }
            
            context.OwinContext.Set("as:client_id", clientId);
            context.Validated(clientId);
        }

        /// <summary>
        /// 客户端授权[生成access token]
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task GrantClientCredentials(OAuthGrantClientCredentialsContext context)
        {
            var oAuthIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
            oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, context.OwinContext.Get<string>("as:client_id")));
            var ticket = new AuthenticationTicket(oAuthIdentity, new AuthenticationProperties { AllowRefresh = true });
            context.Validated(ticket);
            return base.GrantClientCredentials(context);
        }

        /// <summary>
        /// password授权[生成access token]
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            if (!GetUser(context.UserName, context.Password))
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, context.UserName));
            identity.AddClaim(new Claim(ClaimTypes.Role, "user"));
            identity.AddClaim(new Claim("sub", context.UserName));

            var props = new AuthenticationProperties(new Dictionary<string, string>
            {
                {
                    "as:client_id", context.ClientId ?? string.Empty
                },
                {
                    "userName", context.UserName
                }
            });

            var ticket = new AuthenticationTicket(identity, props);
            context.Validated(ticket);
        }

        //clientId != "test" && clientSecret != "1234")
        public bool GetUser(string name, string password)
        {
            if (ConfigurationManager.AppSettings["Admin"].Contains(";" + name + "," + password +";"))
            {
                return true;
            }
            return false;
        }
    }
}