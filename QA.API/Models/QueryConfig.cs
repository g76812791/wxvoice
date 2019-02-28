using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QA.API.Models
{
    [Serializable]
    public class QueryConfig
    {
        public Dictionary<string, DomainMapItem> DomainMap { get; set; }
        public QueryConfig()
        {
            if (DomainMap == null)
                DomainMap = new Dictionary<string, DomainMapItem>();
        }
        public QueryConfig GetQueryConfig()
        {
            QueryConfig qcConfig;

            QueryConfig qc = null;
            try
            {
                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.Load(HttpContext.Current.Server.MapPath("~/App_Data/config/QueryConfig.xml"));
                var xmlNode = CBase.Common.XmlHelper.Node(xmlDoc, "root");
                string strJson = CBase.Common.XmlHelper.GetValue(xmlNode, "/");
                if (string.IsNullOrEmpty(strJson))
                {
                    return null;
                }
                qc = CBase.Common.Serializer.FromJson<QueryConfig>(strJson);
            }
            catch
            {
                CBase.Log.Logger.Error("加载queryconfig对象失败 路径：App_Data/config/QueryConfig.xml");
            }
            qcConfig = qc;
            return qcConfig;

        }
    }

    public class DomainMapItem
    {
        public List<string> KB { get; set; }
        public List<string> FAQ { get; set; }
        public List<string> SG { get; set; }
    }
}