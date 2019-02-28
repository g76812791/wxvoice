using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
namespace QA.web.Tool
{
    public class TextConvert
    {
        public static int StringToInt(string sNumber, int defaultValue)
        {
            if (string.IsNullOrEmpty(sNumber))
                return defaultValue;

            int num = 0;

            return int.TryParse(sNumber, out num) ? num : defaultValue;
        }

        public static string UrlDecode(string q)
        {
            string str = q.Replace("+", "%2B");
            str = HttpContext.Current.Server.UrlDecode(q);
            return str;
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="input">需要加密的字符串</param>
        /// <param name="encode">字符的编码</param>
        /// <returns></returns>
        public static string MD5Encrypt(string input, Encoding encode)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(encode.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x").PadLeft(2, '0'));
            }
            return sBuilder.ToString();
        }

        public static string ToMD5String(string str)
        {
            return MD5Encrypt(str, Encoding.GetEncoding("utf-8"));
        }
    }
}