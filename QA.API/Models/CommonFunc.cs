using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

using System.Net;
using System.Threading.Tasks;
using System.Globalization;
using System.Net.Http;
using QA.Domain.CMeta;
using CBase.DB;
using CBase.DB.Entity;
using QA.Domain.CModel;
using CService.QA.Collector;

namespace QA.API.Models
{
    public static class CommonFunc
    {
        private static readonly string MarkRedStartTag = "###";
        private static readonly string MarkRedEndTag = "$$$";
        private static readonly string MarkRedStartHTML = "<font color=\"#FF0000\">";
        private static readonly string MarkRedEndHTML = "</font>";

        public static Dictionary<string, string> GetFiledsContent(Dictionary<string, string> fieldsMap,
            string focusField, CBase.DB.Entity.QEntity mitem)
        {
            if (fieldsMap == null)
                return null;

            int i = 0;
            StringBuilder tabs = new StringBuilder();
            StringBuilder contents = new StringBuilder();

            foreach (var tab in fieldsMap)
            {
                if (focusField.Contains(tab.Key))
                {
                    continue;
                }
                if (!mitem.FieldValue.ContainsKey(tab.Key))
                { continue; }

                tabs.AppendFormat("<li class=\"tabs-nav-li\" data-tid=\"{0}\">{1}</li><li class=\"tabs-nav-li listop\"></li>",
                    i.ToString(), tab.Value);
                contents.AppendFormat("<div class=\"tabs-content\"><p>{0}</p></div>",
                    CommonFunc.CuttingMore(200, mitem.FieldValue[tab.Key].ToString()));

                i++;
            }
            Dictionary<string, string> reDict = new Dictionary<string, string>();
            reDict.Add("tabs", tabs.ToString());
            reDict.Add("contents", contents.ToString());

            return reDict;

        }

        public static string MarkRed(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            //str = str.Replace("###", MarkRedStartHTML);
            //str = str.Replace("$$$", MarkRedEndHTML);

            //str = ReplaceHtmlTag(str);

            int start = 0;
            int end = 0;
            int curIndex = 0;
            do
            {
                start = str.IndexOf(MarkRedStartTag, curIndex);

                end = str.IndexOf(MarkRedEndTag, curIndex);
                if (start < 0 && end < 0)
                {
                    break;
                }

                if (start < 0)
                {
                    str = str.Remove(end) + str.Substring(end + MarkRedEndTag.Length);
                    curIndex = end;
                }
                if (end < 0)
                {
                    str = str.Remove(start) + str.Substring(start + MarkRedStartTag.Length);
                    curIndex = start;
                }
                if (start >= 0 && end >= 0)
                {
                    if (start > end)
                    {
                        str = str.Remove(end) + str.Substring(end + MarkRedEndTag.Length);
                        curIndex = end;
                    }
                    else
                    {
                        int tmp = str.IndexOf(MarkRedStartTag, start + MarkRedStartTag.Length);
                        if (tmp >= 0 && tmp < end)
                        {
                            str = str.Remove(start) + str.Substring(start + MarkRedStartTag.Length);
                            curIndex = start;
                        }
                        else
                        {
                            curIndex = end + MarkRedEndTag.Length;
                        }
                    }
                }
            } while (start >= 0 || end >= 0);

            return str.Replace(MarkRedStartTag, MarkRedStartHTML).Replace(MarkRedEndTag, MarkRedEndHTML);

        }

        public static string ClearMarkRed(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            return str.Replace(MarkRedStartTag, string.Empty).Replace(MarkRedEndTag, string.Empty);

        }
        /// <summary>
        /// 全部返回，隐藏更多部分，一般适用于不需要保留html标签的文本替换
        /// </summary>
        /// <returns></returns>
        public static string CuttingMore(int count, string content)
        {
            //content = content.Replace("<div>", string.Empty).
            //    Replace("<DIV>", string.Empty).
            //    Replace("</div>", "</br>").
            //    Replace("</DIV>", "</br>");

            List<string> cutList = Cutting(count, content);

            if (cutList == null)
                return string.Empty;


            StringBuilder reHTML = new StringBuilder();

            string preHtml = "<span>{0}</span>";

            string postHtml = @"<span class=""msgmore"" style=""display: none"">{0}</span>
                    <a class=""more_tag"" href=""javascript:void(0)""></a>";


            reHTML.AppendFormat(preHtml, MarkRed(cutList[0]));

            if (cutList.Count == 2 && !string.IsNullOrEmpty(cutList[1]))
                reHTML.AppendFormat(postHtml, MarkRed(cutList[1]));

            return reHTML.ToString();

        }

        /// <summary>
        /// 切分截取包含html标签的文本内容
        /// </summary>
        /// <param name="length"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<string> CuttingHasHTML(int length, string text)
        {
            List<string> reList = new List<string>();
            string Pattern = null;
            MatchCollection m = null;
            StringBuilder resultPre = new StringBuilder();
            string result_end = string.Empty;
            int n = 0;
            char temp;
            bool isCode = false; //是不是HTML代码
            bool isHTML = false; //是不是HTML特殊字符,如&nbsp;
            char[] pchar = text.ToCharArray();
            int i = 0;
            for (i = 0; i < pchar.Length; i++)
            {
                temp = pchar[i];
                if (temp == '<')
                {
                    isCode = true;
                }
                else if (temp == '&')
                {
                    isHTML = true;
                }
                else if (temp == '>' && isCode)
                {
                    n = n - 1;
                    isCode = false;
                }
                else if (temp == ';' && isHTML)
                {
                    isHTML = false;
                }

                if (!isCode && !isHTML)
                {
                    n = n + 1;
                    //UNICODE码字符占两个字节
                    if (System.Text.Encoding.Default.GetBytes(temp + "").Length > 1)
                    {
                        n = n + 1;
                    }
                }

                resultPre.Append(temp);
                if (n >= length)
                {
                    break;
                }
            }
            
            if (i < pchar.Length - 1)
            {
                result_end = text.Substring(i);
            }
            //result.Append(end);

            if(resultPre.Length>0)
            {

            }
            //取出截取字符串中的HTML标记
            //<[^>]+>
            //string temp_result = resultPre.ToString().Replace("(>)[^<>]*(<?)", "$1$2");

            string temp_result = System.Text.RegularExpressions.Regex.Replace(resultPre.ToString(),"(>)[^<>]*(<?)", "$1$2");
            //去掉不需要结素标记的HTML标记
            temp_result = System.Text.RegularExpressions.Regex.Replace(temp_result,@"</?(AREA|BASE|BASEFONT|BR|COL|COLGROUP|DD|DT|FRAME|HR

|IMG|INPUT|ISINDEX|LI|LINK|META|OPTION|PARAM)[^<>]*/?>",
             "",RegexOptions.IgnoreCase);
            //去掉成对的HTML标记
            temp_result = System.Text.RegularExpressions.Regex.Replace(temp_result,@"<([a-zA-Z]+)[^<>]*>(.*?)</\1>", "");
            //用正则表达式取出标记
            Pattern = ("<([a-zA-Z]+)[^<>]*>");
            m = Regex.Matches(temp_result, Pattern);
            ArrayList endHTML = new ArrayList();
            foreach (Match mt in m)
            {
                endHTML.Add(mt.Result("$1"));
            }
            //补全不成对的HTML标记
            for (int j = endHTML.Count - 1; j >= 0; j--)
            {
                resultPre.Append("</");
                resultPre.Append(endHTML[j]);
                resultPre.Append(">");
            }
            reList.Add(resultPre.ToString());
            //后半部做html标签补全
            if (!string.IsNullOrEmpty(result_end))
            {                
                //补全不成对的HTML标记
                for (int j = endHTML.Count - 1; j >= 0; j--)
                {
                    result_end = "<" + endHTML[j] + ">" + result_end;
                }
                reList.Add(result_end);
            }

            return reList;
        }

       

        /// <summary>
        /// 切分截取包含html标签的文本内容
        /// </summary>
        /// <param name="count"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string CuttingMoreHasHTML(int count, string content)
        {

            List<string> cutList = CuttingHasHTML(count, content);

            if (cutList == null)
                return string.Empty;


            StringBuilder reHTML = new StringBuilder();

            string preHtml = "<span>{0}</span>";

            string postHtml = @"<span class=""msgmore"" style=""display: none"">{0}</span>
                    <a class=""more_tag"" href=""javascript:void(0)""></a>";


            reHTML.AppendFormat(preHtml, cutList[0]);

            if (cutList.Count == 2 && !string.IsNullOrEmpty(cutList[1]))
                reHTML.AppendFormat(postHtml, cutList[1]);

            return reHTML.ToString();

        }

        /// <summary>
        /// 部分返回 一般适用于不需要保留html标签的文本替换
        /// </summary>
        /// <returns></returns>
        public static string GetPreContent(int count, string content)
        {
            List<string> cutList = Cutting(count, content);
            return cutList == null ? content : cutList[0];
        }
        /// <summary>
        /// 给出内容切开 不影响标红 一般适用于不需要保留html标签的文本替换
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static List<string> Cutting(int startIndex, string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            content = ReplaceHtmlTag(content);

            if (startIndex >= content.Length)
                return new List<string>() { content, string.Empty };

            if (content.IndexOf(MarkRedStartTag) < 0)
            {
                return new List<string>() { content.Remove(startIndex),
                    content.Substring(startIndex) };
            }

            if (startIndex >= content.Replace(MarkRedStartTag, string.Empty)
                .Replace(MarkRedEndTag, string.Empty).Length)
            {
                return new List<string>() { content, string.Empty };
            }

            List<string> reList = new List<string>();

            int countStartTag = 0;
            int countEndTag = 0;
            int cutIndex = 0;

            for (int i = 0; i < startIndex; i++)
            {
                if ((cutIndex + MarkRedStartTag.Length) < content.Length &&
                    content.Substring(cutIndex, MarkRedStartTag.Length) == MarkRedStartTag)
                {
                    countStartTag++;
                    cutIndex += MarkRedStartTag.Length;
                }

                if ((cutIndex + MarkRedEndTag.Length) < content.Length &&
                    content.Substring(cutIndex, MarkRedEndTag.Length) == MarkRedEndTag)
                {
                    countEndTag++;
                    cutIndex += MarkRedEndTag.Length;
                }

                cutIndex++;
            }

            string preContent = content.Remove(cutIndex);
            string endContent = content.Substring(cutIndex);
            if (countStartTag != countEndTag)
            {
                preContent += MarkRedEndTag;
                endContent = MarkRedStartTag + endContent;
            }

            reList.Add(preContent);
            reList.Add(endContent);

            return reList;

        }

        public static string GetDomainName(string url)
        {
            string result = null;
            url = FormatURL(url);
            try
            {
                HttpRequest request = new HttpRequest(string.Empty, url, string.Empty);
                result = request.Url.Host;
            }
            catch
            {
                result = url;
            }
            return result;
        }

        public static string FormatURL(string url)
        {
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == false ?
                "http://" + url : url;

        }

        public static string RenderString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            s = ReplaceHtmlTag(s);

            return MarkRed(s);

        }

        /// <summary>
        /// 清除所有html标签
        /// </summary>
        /// <param name="html"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ReplaceHtmlTag(string html, int length = 0)
        {
            string strText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            strText = System.Text.RegularExpressions.Regex.Replace(strText, "&[^;]+;", "");

            if (length > 0 && strText.Length > length)
                return strText.Substring(0, length);

            return strText;
        }

        /// <summary>
        /// 清除多个指定html标签 tag="a,p,img"
        /// </summary>
        /// <param name="html"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static string ClearHtmlMutiTag(string html, string tag)
        {
            string[] tagArr = tag.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in tagArr)
            {
                html = System.Text.RegularExpressions.Regex.Replace(html, "</?" + item + "[^>]*>", "", RegexOptions.IgnoreCase);
            }
            return html;
        }

        /// <summary>
        /// 清除指定html标签 tag="a"
        /// </summary>
        /// <param name="html"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static string ClearHtmlTag(string html, string tag)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "</?"+tag+"[^>]*>", "", RegexOptions.IgnoreCase);
        }

        public static string ReplaceAngleBrackets(string html)
        {
           
            if (string.IsNullOrEmpty(html))
            {
                return "";
            }
            string strText = html.Replace("<", "&lt").Replace(">","&gt");
            return strText;
        }

        /// <summary>
        /// 按字节长度截取字符串(支持截取带HTML代码样式的字符串)
        /// </summary>
        /// <param name=”param”>将要截取的字符串参数</param>
        /// <param name=”length”>截取的字节长度</param>
        /// <param name=”end”>字符串末尾补上的字符串</param>
        /// <returns>返回截取后的字符串</returns>
        public static string SubstringToHTML(string param, int length, string end)
        {
            string Pattern = null;
            MatchCollection m = null;
            StringBuilder result = new StringBuilder();
            int n = 0;
            char temp;
            bool isCode = false; //是不是HTML代码
            bool isHTML = false; //是不是HTML特殊字符,如&nbsp;
            char[] pchar = param.ToCharArray();
            for (int i = 0; i < pchar.Length; i++)
            {
                temp = pchar[i];
                if (temp == '<')
                {
                    isCode = true;
                }
                else if (temp == '&')
                {
                    isHTML = true;
                }
                else if (temp == '>' && isCode)
                {
                    n = n - 1;
                    isCode = false;
                }
                else if (temp == ';' && isHTML)
                {
                    isHTML = false;
                }

                if (!isCode && !isHTML)
                {
                    n = n + 1;
                    //UNICODE码字符占两个字节
                    if (System.Text.Encoding.Default.GetBytes(temp + "").Length > 1)
                    {
                        n = n + 1;
                    }
                }

                result.Append(temp);
                if (n >= length)
                {
                    break;
                }
            }
            result.Append(end);
            //取出截取字符串中的HTML标记
            string temp_result = result.ToString().Replace("(>)[^<>]*(<?)", "$1$2");
            //去掉不需要结素标记的HTML标记
            temp_result = temp_result.Replace(@"</?(AREA|BASE|BASEFONT|BODY|BR|COL|COLGROUP|DD|DT|FRAME|HEAD|HR|HTML

|IMG|INPUT|ISINDEX|LI|LINK|META|OPTION|P|PARAM|TBODY|TD|TFOOT|TH|THEAD

|TR|area|base|basefont|body|br|col|colgroup|dd|dt|frame|head|hr|html|img|input|isindex|li|link|meta

|option|p|param|tbody|td|tfoot|th|thead|tr)[^<>]*/?>",
             "");
            //去掉成对的HTML标记
            temp_result = temp_result.Replace(@"<([a-zA-Z]+)[^<>]*>(.*?)</1>", "$2");
            //用正则表达式取出标记
            Pattern = ("<([a-zA-Z]+)[^<>]*>");
            m = Regex.Matches(temp_result, Pattern);
            ArrayList endHTML = new ArrayList();
            foreach (Match mt in m)
            {
                endHTML.Add(mt.Result("$1"));
            }
            //补全不成对的HTML标记
            for (int i = endHTML.Count - 1; i >= 0; i--)
            {
                result.Append("</");
                result.Append(endHTML[i]);
                result.Append(">");
            }
            return result.ToString();
        }



        public static async Task<string> GetDynamicShortsnap(string fulltext, string query)
        {
            string url = "http://192.168.106.253/api/abstract/dynamic/s";

            var handler = new HttpClientHandler()
            {   //AutomaticDecompression = DecompressionMethods.GZip
            };

            string result = string.Empty;
            using (var http = new HttpClient(handler))
            {
                http.DefaultRequestHeaders.Add("charset", "UTF-8");

                var content = new FormUrlEncodedContent(new Dictionary<string, string>()       
                {  
                      {"fulltext",fulltext},
                     {"query",query},
                 });

                var response = await http.PostAsync(url, content);
                response.Headers.Add("charset", "UTF-8");
                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsStringAsync();
                // result = ToGB2312(result);
                // result = System.Text.UnicodeEncoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());

            }
            //var o = BaseService.Base.ProjectTool.ApiHelper.Get(url,
            //    new { 
            //        fulltext = "abc",
            //        query = "a" 
            //    });
            //if (o.code == 1)
            //{
            //    result = (string)BaseService.Base.ProjectTool.ApiHelper.Response(o);
            //}
            return result;
        }

        /// <summary>
        /// 将Unicode编码转换为汉字字符串
        /// </summary>
        /// <param name="str">Unicode编码字符串</param>
        /// <returns>汉字字符串</returns>
        public static string ToGB2312(string str)
        {
            string r = "";
            MatchCollection mc = Regex.Matches(str, @"\\u([\w]{2})([\w]{2})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            byte[] bts = new byte[2];
            foreach (Match m in mc)
            {
                bts[0] = (byte)int.Parse(m.Groups[2].Value, NumberStyles.HexNumber);
                bts[1] = (byte)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
                r += Encoding.Unicode.GetString(bts);
            }
            return r;
        }

        public static string SplitSection(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            content = ReplaceAngleBrackets(content); //转义尖括号
            content = "<p>" + content + "</p>";
            content = ReplaceCLRF(content,"</p><p>"); //回车换行替换为br

            content = content.Replace(";;", ";");
            content = content.Replace("::", ":");
            content = content.Replace("：：", "：");
            content = content.Replace("；；", "；");

            int len = content.Length;
            StringBuilder sbContent = new StringBuilder();
            int lastPos = 0;
            for (int i = 0; i < len; i++)
            {
                sbContent.Append(content[i]);
                switch (content[i])
                {
                    case ':':
                    case '：':
                        if (i > 0 )
                        {
                            string preChar = GetPreChar(content, i);
                            if(!string.IsNullOrEmpty(preChar) && IsChinese(preChar[0]) && IsInBrackets(content,i)==false)
                            {
                                
                                sbContent.Append("</p><p>");
                            }
                        }
                        lastPos = i;
                        break;

                    case ';':
                    case '；':
                        if (i > 0)
                        {
                            if (i > lastPos + 15)
                            {
                                sbContent.Append("</p><p>");
                            }
                            else if (i > lastPos + 3)
                            {
                                if (CheckContainsNum(content.Substring(lastPos, 3)))
                                {
                                    sbContent.Append("</p><p>");
                                }
                            }
                        }
                        lastPos = i;
                        break;
                    default:
                        break;
                }

            }

            return sbContent.ToString();
            //return "<p>" + Regex.Replace(content, @"(:|;|：|；)", "$1</p><p>",
            //   RegexOptions.Compiled | RegexOptions.IgnoreCase) + "</p>";
           
            
        }
        /// <summary>
        /// 取指定位置的前一个非空字符 且不是$$$标红标记
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static string GetPreChar(string c, int p)
        {
            
            while (p > 0)
            {
                if (c[p - 1] == ' ' || c[p - 1] == '$')
                {                   
                    p--;
                }
                else
                {
                    return c[p - 1].ToString();
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// 判断是否在括号里面
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static bool IsInBrackets(string c, int p)
        {
            int i = 20;
            while (p > 0 && i > 0)
            {
                if (c[p - 1] == '《' || c[p - 1] == '（' || c[p - 1] == '(' || c[p - 1] == '【' || c[p - 1] == '[')
                    return true;
                else
                {
                    p--;
                    i--;
                }
            }
            return false;
        }
        public static bool CheckContainsNum(string text)
        {
            return text.IndexOfAny(
                new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
                    '一', '二', '三', '四', '五', '六', '七', '八', '九', '十'}
                    )>=0;
        }
        public static bool IsChinese(char s)
        {
            if (s > 0x4E00 && s < 0x9FA5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static string ReplaceCLRF(string str, string nstr = "<br />")
        {
            return str.Replace("\r", nstr).Replace("\n", nstr);
        }
        public static Dictionary<string, Dictionary<string, Dictionary<string, object>>> BuildDict(
            string ordinate, string abscissa, CMKArea cArea)
        {
            if (cArea == null || cArea.KNode.Count == 0)
                return null;

            Dictionary<string, Dictionary<string, Dictionary<string, object>>> dict = 
                new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();

            //装载第一个node 并以第一个为纵坐标为标准添加后续节点
            foreach (var data in cArea.KNode[0].DATA)
            {
                string ordinateValue = string.Empty;
                string abscissaValue = string.Empty;

                if (data.FieldValue.ContainsKey(ordinate) && data.FieldValue[ordinate] != null)
                {
                    ordinateValue = data.FieldValue[ordinate].ToString();
                }
                if (data.FieldValue.ContainsKey(abscissa) && data.FieldValue[abscissa] != null)
                {
                    abscissaValue = data.FieldValue[abscissa].ToString();
                    cArea.KNode[0].Title = abscissaValue;
                }

                if (!string.IsNullOrEmpty(ordinateValue) && !string.IsNullOrEmpty(abscissaValue))
                {
                    var abscissaDic = new Dictionary<string, Dictionary<string, object>>();
                    abscissaDic.Add(abscissaValue, data.FieldValue);
                    if (!dict.ContainsKey(ordinateValue))
                    {
                        dict.Add(ordinateValue, abscissaDic);
                    }
                }
            }

            //添加后续节点
            for (int i = 1; i < cArea.KNode.Count; i++)
			{
                foreach (var data in cArea.KNode[i].DATA)
                {
                    string ordinateValue = string.Empty;
                    string abscissaValue = string.Empty;

                    if (data.FieldValue.ContainsKey(ordinate) && data.FieldValue[ordinate] != null)
                    {
                        ordinateValue = data.FieldValue[ordinate].ToString();
                    }
                    if (!dict.ContainsKey(ordinateValue))
                        continue;
                        
                    if (data.FieldValue.ContainsKey(abscissa) && data.FieldValue[abscissa] != null)
                    {
                        abscissaValue = data.FieldValue[abscissa].ToString();
                        cArea.KNode[i].Title = abscissaValue;
                    }

                    if (!string.IsNullOrEmpty(ordinateValue) && !string.IsNullOrEmpty(abscissaValue))
                    {
                        var abscissaDic = new Dictionary<string, Dictionary<string, object>>();
                        abscissaDic.Add(abscissaValue, data.FieldValue);
                        var ordinateDic = dict[ordinateValue];
                        if (!ordinateDic.ContainsKey(abscissaValue))
                        {
                            ordinateDic.Add(abscissaValue, data.FieldValue);
                        }                        
                    }
                }
			}

            
            return dict;

        }

        /// <summary>
        /// 根据sql返回查询结果集
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static QueryContainer<QEntity> GetResult(string sql,bool isNotMarkRed = false)
        {
            return new KB().GetSQLQC(sql, isNotMarkRed);
        }

        /// <summary>
        /// 返回制定查询实体的 单个字段内容 或者 string.Empty
        /// </summary>
        /// <param name="qe"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string GetValue(QEntity qe,string field)
        {
            string re = string.Empty;
            if (qe != null && qe.FieldValue != null && qe.FieldValue.ContainsKey(field))
            {
                re = qe.FieldValue[field].ToString(); 
            }
            if (re == null)
                re = string.Empty;
            return re;
        }

        public static List<T> RandomSortList<T>(List<T> ListT, int n = 5)
        {
            if (ListT == null||ListT.Count==0)
            {
                return new List<T>();
            }
            Random random = new Random();
            List<T> newList = new List<T>();
            if (n>ListT.Count)
            {
                n = ListT.Count;
            }
            List<int> result = new List<int>();
            int temp;
            while (result.Count < n)
            {
                temp = random.Next(0, ListT.Count);
                if (!result.Contains(temp))
                {
                    result.Add(temp);
                    newList.Add(ListT[temp]);
                }
            }
            return newList;
        }
        /// <summary>
        /// 写内容收集日志
        /// </summary>
        /// <param name="cinfo"></param>
        /// <param name="q"></param>
        /// <param name="logType"></param>
        public static bool WriteContentLog(ClientInfo cinfo, string q)
        {
            var client = new CollectorInterfaceClient();
            Task.Run<bool>(() =>
            {
                return client.SetConfused(new CollectorEntity()
                {
                    Content = q,
                    Type = (int)cinfo.Type,
                    IP = cinfo.IP,
                    UserID = cinfo.UserName
                });
            });
            return true;
        }
    }
}