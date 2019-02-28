using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace QA.API.Controllers
{
    public class WXController : Controller
    {
        [ActionName("Index")]
        [HttpGet]
        public ActionResult Get()
        {
            string token = "weichat";

            string echoStr = Request.QueryString["echoStr"];//随机字符串 
            string signature = Request.QueryString["signature"];//微信加密签名
            string timestamp = Request.QueryString["timestamp"];//时间戳 
            string nonce = Request.QueryString["nonce"];//随机数 
            string[] ArrTmp = { token, timestamp, nonce };
            Array.Sort(ArrTmp);     //字典排序
            string tmpStr = string.Join("", ArrTmp);
            tmpStr = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(tmpStr, "SHA1");
            tmpStr = tmpStr.ToLower();
            if (tmpStr == signature)
            {
                return Content(echoStr);
            }
            else
            {
                return Content("false");
            }
        }
        [ActionName("Index")]
        [HttpPost]
        public ActionResult Post()
        {
            //接收数据
            System.IO.StreamReader reader = new System.IO.StreamReader(Request.InputStream);
            String xmlData = reader.ReadToEnd();
            XElement xdoc = XElement.Parse(xmlData);

            //解析XML
            var toUserName = xdoc.Element("ToUserName").Value;
            var fromUserName = xdoc.Element("FromUserName").Value;
            //var createTime = xdoc.Element("CreateTime").Value;
            var msgtype = xdoc.Element("MsgType").Value;

            string content = "";
            if (msgtype=="voice")
            {
                content = xdoc.Element("Recognition").Value; 
            }
            else if (msgtype == "text")
            {
                content = xdoc.Element("Content").Value;
            }
            if (content!="")
            {
                content = content.TrimEnd('。');
            }
            //DateTime datatime = DateTime.Now;
            int datetime = 1460541339;
            //回复内容
            StringBuilder resxml = new StringBuilder(
            string.Format("<xml><ToUserName><![CDATA[{0}]]></ToUserName><FromUserName><![CDATA[{1}]]></FromUserName><CreateTime>{2}</CreateTime>", fromUserName, toUserName, datetime));
            resxml.AppendFormat("<MsgType><![CDATA[text]]></MsgType><Content><![CDATA[{0}]]></Content><FuncFlag>0</FuncFlag></xml>","API:" +content);
            string msg = resxml.ToString();
            return Content(msg);
        }

    }
}