using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;
using NAudio.Wave;
using Newtonsoft.Json;
using Voice.Comm;
using System.Web;

namespace Voice.Controllers
{
    public class GetTextController : ApiController
    {
        [HttpPost]
        public HttpResponseMessage Post()
        {

            var file = HttpContext.Current.Request.Files.Count > 0 ?HttpContext.Current.Request.Files[0] : null;
            if (file != null && file.ContentLength > 0)
            {
               /* var fileName = Path.GetFileName(file.FileName);

                var path = Path.Combine(
                    HttpContext.Current.Server.MapPath("~/uploads"),
                    fileName
                );
                file.SaveAs(path);*/

                Mp3FileReader mp3 = new Mp3FileReader(file.InputStream);
                var d = WaveFormatConversionStream.CreatePcmStream(mp3);
                //  FileStream data = System.IO.File.OpenRead(@"D:\16k.pcm");
                var result = BaiduAi.Instance.Recognize(d, "CF619BC0-C9F2-4057-A9FA-8BC3D1A40B58", "wav", 16000, 1536);
                string json = JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
                return new HttpResponseMessage
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }
            else
            {
                return new HttpResponseMessage
                {
                    Content = new StringContent("{\"err_msg\": \"Error\",\"tip\":\"声音读取失败\"}", System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
