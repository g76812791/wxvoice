using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QA.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.Formatters.Add(new PlainTextTypeFormatter());

            config.Formatters.Add(new InkeyJsonMediaTypeFormatter());
            //config.Formatters.Insert(0, new InkeyJsonMediaTypeFormatter());

            var format = GlobalConfiguration.Configuration.Formatters;
            //清除默认xml
            format.XmlFormatter.SupportedMediaTypes.Clear();
            //通过参数设置返回格式
            format.JsonFormatter.MediaTypeMappings.Add(new QueryStringMapping("t", "json", "application/json"));
            format.XmlFormatter.MediaTypeMappings.Add(new QueryStringMapping("t", "xml", "application/xml"));
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new IsoDateTimeConverter
            {
                DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss"
            });

        }


        #region 媒体格式

        public class InkeyJsonMediaTypeFormatter : BaseJsonMediaTypeFormatter
        {
            public InkeyJsonMediaTypeFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/octet-stream"));
                MediaTypeMappings.Add(new InkeyJsonHttpRequestHeaderMapping());
                var jsondate = new JsonSerializerSettings();
                jsondate.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                SerializerSettings = jsondate;
            }

            public override JsonReader CreateJsonReader(Type type, Stream readStream,
                Encoding effectiveEncoding)
            {
                return new JsonTextReader(new StreamReader(readStream, effectiveEncoding));
            }

            public override JsonWriter CreateJsonWriter(Type type, Stream writeStream,
                Encoding effectiveEncoding)
            {
                return new JsonTextWriter(new StreamWriter(writeStream, effectiveEncoding));
            }
        }

        public class InkeyJsonHttpRequestHeaderMapping : RequestHeaderMapping
        {

            public InkeyJsonHttpRequestHeaderMapping()
                : base(
                    @"x-requested-with", @"XMLHttpRequest", StringComparison.OrdinalIgnoreCase, isValueSubstring: true,
                    mediaType: "application/octet-stream")
            {
            }

            public override double TryMatchMediaType(HttpRequestMessage request)
            {

                if (request.Headers.Accept.Count == 0
                    ||
                    (request.Headers.Accept.Count == 1 &&
                     request.Headers.Accept.First().MediaType.Equals("*/*", StringComparison.Ordinal)))
                {
                    return base.TryMatchMediaType(request);
                }
                else
                {
                    return 0.0;
                }
            }

        }

        public class PlainTextTypeFormatter : BaseJsonMediaTypeFormatter
        {
            public PlainTextTypeFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
                MediaTypeMappings.Add(new PlainTextHttpRequestHeaderMapping());
                var jsondate = new JsonSerializerSettings();
                jsondate.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                SerializerSettings = jsondate;
            }

            public override JsonReader CreateJsonReader(Type type, Stream readStream,
                Encoding effectiveEncoding)
            {
                return new JsonTextReader(new StreamReader(readStream, effectiveEncoding));
            }

            public override JsonWriter CreateJsonWriter(Type type, Stream writeStream,
                Encoding effectiveEncoding)
            {
                return new JsonTextWriter(new StreamWriter(writeStream, effectiveEncoding));
            }
        }

        public class PlainTextHttpRequestHeaderMapping : RequestHeaderMapping
        {
            public PlainTextHttpRequestHeaderMapping()
                : base(
                    @"x-requested-with", @"XMLHttpRequest", StringComparison.OrdinalIgnoreCase, isValueSubstring: true,
                    mediaType: "text/plain")
            {
            }

            public override double TryMatchMediaType(HttpRequestMessage request)
            {

                if (request.Headers.Accept.Count == 0
                    ||
                    (request.Headers.Accept.Count == 1 &&
                     request.Headers.Accept.First().MediaType.Equals("*/*", StringComparison.Ordinal)))
                {
                    return base.TryMatchMediaType(request);
                }
                else
                {
                    return 0.0;
                }
            }
        }

        #endregion


    }
}
