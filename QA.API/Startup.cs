﻿using System;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Owin;
using QA.API;
using QA.API.OAuth;


[assembly: OwinStartup(typeof(Startup))]
namespace QA.API
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 有关如何配置应用程序的详细信息，请访问 http://go.microsoft.com/fwlink/?LinkID=316888
//          var config = new HttpConfiguration();
//           WebApiConfig.Register(config);
            //开启OAuth服务
            ConfigureOAuth(app);
//            app.UseWebApi(config);

          
        }

        public void ConfigureOAuth(IAppBuilder app)
        {
            // Token 生成配置
            var oAuthOptions = new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true, //允许客户端使用Http协议请求
                AuthenticationMode = AuthenticationMode.Active,
                TokenEndpointPath = new PathString("/token"), //请求地址
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(1), //token过期时间

                //提供认证策略
                Provider = new OpenAuthorizationServerProvider(),
                RefreshTokenProvider = new SimpleRefreshTokenProvider()
            };
            app.UseOAuthBearerTokens(oAuthOptions);
        }
    }
}