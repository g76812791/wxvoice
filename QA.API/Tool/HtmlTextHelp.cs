using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.IO;
using CsQuery;
using CsQuery.Implementation;

namespace QA.web.Tool
{
    public class HtmlTextHelp : Controller
    {
        public string GetText(string lujing)
        {
            string path = Server.MapPath(lujing);
            string htmltext = string.Empty;
            if (Directory.Exists(path))
            {
                //new RazorView().Render(this.ControllerContext.
                try
                {
                    using (StreamReader reader = new StreamReader(Server.MapPath(lujing+@"\index.html")))
                    {
                        string temp = reader.ReadToEndAsync().Result;

                        htmltext =RenderCQ(temp);
                    }
                }
                catch { }
            }
            else
            {
                //不存在
            }
            return htmltext;
        }

        public string ConvertPath(string lujing)
        {
            string path =lujing;
            string htmltext = string.Empty;
            if (Directory.Exists(path))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(lujing + @"\index.html"))
                    {
                        string temp = reader.ReadToEndAsync().Result;
                        htmltext =RenderCQ (temp);
                    }
                }
                catch { }
            }
            else
            {
                //不存在
            }
            return htmltext;
        }


        public string RenderCQ(string html)
        {
            string virtualPath = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath == "/" ? "" : System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;

            CQ DOM = html;
            var csslist = DOM.Document.GetElementsByTagName("link");

            foreach (var css in csslist)
            {
                var s = css.GetAttribute("href");
                // s = lujing + s;
                s = s.Replace("~", virtualPath);
                css.SetAttribute("link", s);
            }
            var script = DOM.Document.GetElementsByTagName("script");
            foreach (var sc in script)
            {
                var s = sc.GetAttribute("src");
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("http"))
                {
                    // s = lujing + s;
                    s = s.Replace("~", virtualPath);
                    sc.SetAttribute("src", s);
                }
            }
            var imgs = DOM.Document.GetElementsByTagName("img");
            foreach (var img in imgs)
            {
                var s = img.GetAttribute("src");
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("http"))
                {
                    // s = lujing + s;
                    s = s.Replace("~", virtualPath);
                    img.SetAttribute("src", s);
                }
            }
            var alink = DOM.Document.GetElementsByTagName("a");
            foreach (var a in alink)
            {
                var url = a.GetAttribute("href");
                if (!string.IsNullOrEmpty(url) && url.StartsWith("~"))
                {
                    url = url.Replace("~", virtualPath);
                    a.SetAttribute("href", url);
                }

                var gurl = a.GetAttribute("ghref");
                if (!string.IsNullOrEmpty(gurl) && gurl.StartsWith("~"))
                {
                    gurl = gurl.Replace("~", virtualPath);
                    a.SetAttribute("ghref", gurl);
                }
            }
            var tempstr = DOM.Render();

            return tempstr;
        }

    }
}