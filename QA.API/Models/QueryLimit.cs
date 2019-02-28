using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QA.API.Models
{
    [Serializable]
    public class QueryLimit
    {
        public Dictionary<string, DomainMapUnit> DomainMap { get; set; }
        public QueryLimit()
        {
            if (DomainMap == null)
                DomainMap = new Dictionary<string, DomainMapUnit>();
        }
        public QueryLimit Get()
        {
            QueryLimit qcConfig;
            string catchKey = "QueryLimitCacheKey";

            QueryLimit qc = CBase.Cache.CacheHelper.GetCache<QueryLimit>(catchKey);

            if (qc == null)
            {
                try
                {
                    System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                    xmlDoc.Load(HttpContext.Current.Server.MapPath("~/App_Data/config/QueryLimit.xml"));
                    var xmlNode = CBase.Common.XmlHelper.Node(xmlDoc, "root");
                    string strJson = CBase.Common.XmlHelper.GetValue(xmlNode, "/");
                    if (string.IsNullOrEmpty(strJson))
                    {
                        return null;
                    }
                    qc = CBase.Common.Serializer.FromJson<QueryLimit>(strJson);
                    CBase.Cache.CacheHelper.SetCache(catchKey,qc);
                }
                catch
                {
                    CBase.Log.Logger.Error("加载queryconfig对象失败 路径：App_Data/config/QueryConfig.xml");
                }
            }
            
            qcConfig = qc;
            return qcConfig;

        }
    }

    /// <summary>
    /// KB       知识域
    /// FAQ      问答集
    /// FAQEX    排除的问答集
    /// SG       自由文本
    /// </summary>
    public class DomainMapUnit
    {
        public List<string> KB { get; set; }
        public List<string> FAQ { get; set; }
        public List<string> FAQEX { get; set; }
        public List<string> SG { get; set; }
    }
}