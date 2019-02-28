using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace QA.web.Tool
{
    /// <summary>
    /// WEBconfig 中全局配置的项  全部使用字符串表示，如果希望是bool 请在ge里转换，1代表true 其他代表false
    /// </summary>
    public static class GlobalConfig
    {
        /// <summary>
        /// 
        /// </summary>
        private static string m_IsVisitMonitor;

        /// <summary>
        /// 是否使用访问控制服务，启用以后会根据后台服务自动形成黑名单，针对黑名单做拦截处理
        /// </summary>
        public static bool IsVisitMonitor
        {
            
            get
            {
                if (m_IsVisitMonitor == null)
                {
                    m_IsVisitMonitor = Get("IsVisitMonitor");
                }
                return m_IsVisitMonitor == "1";
            }            
        }
        

        public static string Get(string key)
        {          
            string text = ConfigurationManager.AppSettings[key];
            if(text == null)
            {
                return string.Empty;
            }
            return text;
        }
    }
}