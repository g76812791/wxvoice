using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace QA.API.Tool
{
    public class ValiAppID
    {
        public static bool IsValid(string appid,string cid,string q)
        {
            var dict = GetValiDict();
            if (dict == null)
                return false;

            string secret = dict.ContainsKey(appid) ? dict[appid] : string.Empty;
            if (string.IsNullOrEmpty(secret))
                return false;
            string encodeStr = QA.web.Tool.TextConvert.ToMD5String(secret + "_" + q);
            if (string.IsNullOrEmpty(encodeStr))
                return false;

            return cid == encodeStr;

        }
        public static Dictionary<string, string> GetValiDict()
        {
            string cacheKey = "valiappCacheKey";
            Dictionary<string, string> dict = CBase.Cache.CacheHelper.GetCache<Dictionary<string,string>>(cacheKey);
            if (dict == null)
            {
                try
                {
                    dict = new Dictionary<string, string>();
                    string keys = ConfigurationManager.AppSettings["APPKeys"];
                    var keyarr = keys.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in keyarr)
                    {
                        var keyvalue = item.Split(':');
                        dict.Add(keyvalue[0], keyvalue[1]);
                    }
                    CBase.Cache.CacheHelper.SetCache(cacheKey, dict);
                }
                catch 
                {}
            }
            return dict;
        }
    }
}