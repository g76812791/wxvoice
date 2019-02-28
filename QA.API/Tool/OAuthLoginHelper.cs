using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;

namespace QA.web.Tool
{
    public class OAuthLoginHelper
    {
        public static void GetCode(string state,string parm)
        {
            string authorizationUrl = GetKey("AuthorizePath");
            var paras = new Dictionary<string, string>();
            paras.Add("client_id", GetKey("appkey"));
            paras.Add("response_type", "token");
            paras.Add("state", state);
            if (!string.IsNullOrEmpty(parm))
            {
                paras.Add("redirect_uri", GetKey("CallBack") + "?parm=" + parm);
            }
            else
            {
                paras.Add("redirect_uri", GetKey("CallBack"));
            }
            
            string requestUrl = AddParametersToURL(authorizationUrl, paras);
            System.Web.HttpContext.Current.Response.Redirect(requestUrl);
        }
        public static string GetTokenParas(string code)
        {
            string url = GetKey("TokenPath");
            var paras = new Dictionary<string, string>();
            paras.Add("client_id", GetKey("appkey"));
            paras.Add("client_secret", GetKey("appsecret"));
            paras.Add("redirect_uri", HttpUtility.UrlEncode(GetKey("CallBack")));
            paras.Add("grant_type", "authorization_code");
            paras.Add("code", code);
            var data = GetParametersString(paras);
            return data;
        }
        public static string GetToken(string code)
        {
            string url = GetKey("TokenPath");
            var paras = new Dictionary<string, string>();
            paras.Add("client_id", GetKey("appkey"));
            paras.Add("client_secret", GetKey("appsecret"));
            paras.Add("redirect_uri", HttpUtility.UrlEncode(GetKey("CallBack")));
            paras.Add("grant_type", "authorization_code");
            paras.Add("code", code);
            var data = GetParametersString(paras);
            var content = Post(url, data);
            var jo = JObject.Parse(content);
            return jo["access_token"].ToString();
        }

        public static string Get(string url)
        {
            var value = string.Empty;
            using (var client = new WebClient())
            {
                value = client.DownloadString(url);
            }
            return value;
        }


        public static string GetUserInfo(string url, string token)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "User/Get_User_Info");
            request.Referer = "http://qa.cnki.net";
            request.Accept = "Accept:text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers["Accept-Language"] = "zh-CN,zh;q=0.";
            request.Headers["Accept-Charset"] = "GBK,utf-8;q=0.7,*;q=0.3";
            request.Headers["Authorization"] = "Bearer " + token;
            request.UserAgent = "User-Agent:Mozilla/5.0 (Windows NT 5.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1";
            request.KeepAlive = true;
            request.ContentType = "application/json";
            request.Method = "GET";
            Encoding encoding = Encoding.UTF8;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, encoding);
            string retString = streamReader.ReadToEnd();
            streamReader.Close();
            responseStream.Close();
            return retString;
        }




        public static string Get(string url, string token)
        {
            var value = string.Empty;
            using (var client = new WebClient())
            {
                string authstr = "Bearer " + token;
                client.Headers.Add("Authorization", authstr);
                // value = client.DownloadString(url);
                value = Encoding.UTF8.GetString(client.DownloadData(url));
            }
            return value;
        }

        public static string Post(string url, string data)
        {
            string value;
            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");//采取POST方式必须加的header，163需要  
                value = client.UploadString(url, data);
            }
            return value;
        }

        public static string GetKey(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static string GetParametersString(Dictionary<string, string> dic)
        {
            string query = string.Empty;
            foreach (var entry in dic)
            {
                query += string.Format("{0}={1}", entry.Key, entry.Value);
                query += "&";
            }
            if (query != string.Empty)
                query = query.TrimEnd(new char[] { '&' });
            return query;
        }

        public static string AddParametersToURL(string url, Dictionary<string, string> dic)
        {
            var query = GetParametersString(dic);
            url += "?" + query;
            return url;
        }
    }
}