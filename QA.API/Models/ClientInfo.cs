using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QA.API.Models
{
    public class ClientInfo
    {
        public string UserName { get; set; }
        public string IP { get; set; }

        public ContentLogType Type { get; set; }
    }



    /// <summary>
    /// 知识库：       
    ///     未识别：1,
    ///     已识别：2,
    ///     超时：4,
    ///句群： 
    ///     相似度高：5,
    ///     相似度低：3,
    ///     超时：6,
    ///问答集：
    ///     超时：7,
    ///用户问答集：
    ///     超时：8,
    ///RPC问题排序：
    ///     超时：9,
    /// </summary>
    public enum ContentLogType
    {
        未识别 = 1,
        已识别 = 2,
        知识库超时 = 4,

        相似度高 = 5,
        相似度低 = 3,
        句群超时 = 6,
        问答集超时 = 7,
        用户问答集超时 = 8,
        问题排序超时 = 9,
        新问题收集 = 10,
        有答案 = 12,
        无答案 = 13,

    }
}