using CBase.DB;
using QA.Domain.CMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CService.QA.Feedback;
using CService.QA.Collector;
using QA.Domain;
using BaseService.ServiceCenter.SystemRuntime;
using QA.API.Models;

namespace QA.web.Tool
{ 
    public class DataSort
    {
        public static QueryDataContainer Sort(QueryContainer<CMFAQ> faq, QueryContainer<CMAnswer> qcAnswer, QueryContainer<CMFAQ_NET> faq_net, Dictionary<string, QueryContainer<CMKArea>> nav, ClientInfo cinfo)
        {            
            int maxScore = 85;
            double coefficient = 0.85;
            QueryDataContainer qdc = new QueryDataContainer()
            {
                MetaList = new List<CMBase>(),
                ElapsedList = new List<FuncElapsed>()
            };

            qdc.ErrorMsg = "";
            var IDs = new List<string>();
            ConvertFAQ(faq, coefficient, qdc, IDs);
            ConvetFAQ_NET(faq_net, coefficient, qdc, IDs);
            ConvertAnswer(qcAnswer, cinfo, maxScore, coefficient, qdc, IDs);
            ConvertNavi(nav, coefficient, qdc, IDs);
          
            qdc.Total = qdc.MetaList.Count;
                        
            AddScoreByEvaluate(cinfo, coefficient, qdc, IDs);          

            var sortlist = (from qd in qdc.MetaList
                            orderby qd.OrderBy descending
                            select qd).ToList();
            qdc.MetaList = sortlist;
            qdc.Total = sortlist.Count;

            return qdc;  

        }

        public static QueryDataContainer SortKB(QueryContainer<CMFAQ> faq,  QueryContainer<CMFAQ_NET> faq_net, Dictionary<string, QueryContainer<CMKArea>> nav, ClientInfo cinfo)
        {            
            double coefficient = 0.85;
            QueryDataContainer qdc = new QueryDataContainer()
            {
                MetaList = new List<CMBase>(),
                ElapsedList = new List<FuncElapsed>()
            };

            qdc.ErrorMsg = "";
            var IDs = new List<string>();
            ConvertFAQ(faq, coefficient, qdc, IDs);
            ConvetFAQ_NET(faq_net, coefficient, qdc, IDs);           
            ConvertNavi(nav, coefficient, qdc, IDs);
            qdc.Total = qdc.MetaList.Count;            

            var sortlist = (from qd in qdc.MetaList
                            orderby qd.OrderBy descending
                            select qd).ToList();
            qdc.MetaList = sortlist;
            qdc.Total = sortlist.Count;

            return qdc;

        }
        public static QueryDataContainer SortSenGroup(QueryContainer<CMAnswer> qcAnswer, ClientInfo cinfo)
        {
            int maxScore = 85;
            double coefficient = 0.85;
            QueryDataContainer qdc = new QueryDataContainer()
            {
                MetaList = new List<CMBase>(),
                ElapsedList = new List<FuncElapsed>()
            };

            qdc.ErrorMsg = "";
            var IDs = new List<string>();           
            ConvertAnswer(qcAnswer, cinfo, maxScore, coefficient, qdc, IDs);
            qdc.Total = qdc.MetaList.Count;
            AddScoreByEvaluate(cinfo, coefficient, qdc, IDs);

            var sortlist = (from qd in qdc.MetaList
                            orderby qd.OrderBy descending
                            select qd).ToList();
            qdc.MetaList = sortlist;
            qdc.Total = sortlist.Count;

            return qdc;  

        }

        /// <summary>
        /// 获取点赞评价修改排序得分
        /// </summary>
        /// <param name="cinfo"></param>
        /// <param name="coefficient"></param>
        /// <param name="qdc"></param>
        /// <param name="IDs"></param>
        private static void AddScoreByEvaluate(ClientInfo cinfo, double coefficient, QueryDataContainer qdc, List<string> IDs)
        {
            try
            {
                Respond rspdata = null;
                double et;
                rspdata = CBase.Log.FuncCatch.Catch<Respond>(delegate
                {

                    return TimedTask.CallActionWithTimeoutSync(200, delegate
                    {
                        var client = new FeedbackServiceInterfaceClient();
                        //Task.Delay(2000).Wait();
                        return client.GetEvaluate(IDs);

                    }, "FeedbackService", cinfo);

                }, out et);

                qdc.ElapsedList.Add(new FuncElapsed("FeedbackService", et));
                if (rspdata == null)
                    return;

                var data = rspdata.Data;
                int max = rspdata.MaxCount;
                if (data != null && max > 10)
                {
                    foreach (var d in data)
                    {
                        var count = d.count;
                        var id = d.ahash;
                        var qdata = qdc.MetaList.Where(p => p.ID == id).FirstOrDefault();
                        if (qdata != null)
                        {
                            qdata.OrderBy = qdata.OrderBy + (1 - coefficient) * (Math.Log(count + 1, Math.E) / Math.Log(max + 1, Math.E));
                            //qdata.OrderBy = qdata.OrderBy + (1 - coefficient) * Math.Log(count + 1, Math.E);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 转换句群
        /// </summary>
        /// <param name="qcAnswer"></param>
        /// <param name="cinfo"></param>
        /// <param name="maxScore"></param>
        /// <param name="coefficient"></param>
        /// <param name="qdc"></param>
        /// <param name="IDs"></param>
        private static void ConvertAnswer(QueryContainer<CMAnswer> qcAnswer, ClientInfo cinfo, int maxScore, double coefficient, QueryDataContainer qdc, List<string> IDs)
        {
            if (qcAnswer != null)
            {
                if (qcAnswer.MetaList != null)
                {
                    int max = 0;
                    foreach (var f in qcAnswer.MetaList)
                    {
                        CMBase cb = new CMBase();
                        cb.Data = f;
                        cb.QExp = qcAnswer.QExp;
                        cb.DataType = DataType.CMAnswer;
                        string id = f.SourceKB + "_" + f.ShortMsg.GetHashCode();
                        cb.ID = id;
                        int score = TextConvert.StringToInt(f.Score, 0);
                        cb.Weight = score / 100.00 * 0.6;
                        cb.OrderBy = coefficient * cb.Weight;

                        if (f.STRATEGY == "QueryLiter")
                        {
                            f.ShortMsg = f.ShortMsg + "...";
                        }

                        qdc.MetaList.Add(cb);
                        IDs.Add(id);
                        if (score > max)
                        {
                            max = score;
                        }
                    }
                    //得分低
                    int type = 3;
                    if (max > maxScore && qcAnswer.MetaList.Count > 0)
                    {   //得分高
                        type = 5;
                    }
                    //收集
                    if (qcAnswer.QExp != null)
                    {
                        Task.Run<bool>(() =>
                        {
                            return new CollectorInterfaceClient().SetConfused(new CollectorEntity()
                            {
                                Content = qcAnswer.QExp.InputText,
                                Type = type,
                                IP = cinfo.IP,
                                UserID = cinfo.UserName
                            });
                        });
                    }

                    qdc.QT_Type = qcAnswer.QType;
                }
                if (!string.IsNullOrEmpty(qcAnswer.ErrorMsg))
                    qdc.ErrorMsg += string.Format("SentenceGroup：{0};", qcAnswer.ErrorMsg);
                qdc.Elapsed += qcAnswer.Elapsed;
                qdc.ElapsedList.Add(new FuncElapsed("SentenceGroup", qcAnswer.Elapsed));


            }
        }

        /// <summary>
        /// 转换知识库
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="coefficient"></param>
        /// <param name="qdc"></param>
        /// <param name="IDs"></param>
        private static void ConvertNavi(Dictionary<string, QueryContainer<CMKArea>> nav, double coefficient, QueryDataContainer qdc, List<string> IDs)
        {
            if (nav != null)
            {
                try
                {
                    foreach (var nvadata in nav.Values)
                    {
                        string naviDomain = string.Empty;
                        var navQC = nvadata;
                        if (navQC.Total > 0)
                        {
                            foreach (var f in navQC.MetaList)
                            {
                                CMBase cb = new CMBase();
                                cb.Data = f;
                                cb.QExp = navQC.QExp;
                                string id = f.Domain + "_" + f.Title.GetHashCode();
                                cb.ID = id;
                                cb.DataType = DataType.CMNav;
                                cb.Weight = 1;
                                cb.OrderBy = coefficient * cb.Weight;
                                qdc.MetaList.Add(cb);
                                IDs.Add(id);
                                naviDomain = f.qt_domain;
                            }
                            qdc.HaveAccurate = true;
                        }
                        if (!string.IsNullOrEmpty(navQC.ErrorMsg))
                            qdc.ErrorMsg += string.Format("Navi：{0};", navQC.ErrorMsg);

                        qdc.Elapsed += navQC.Elapsed;
                        qdc.ElapsedList.Add(new FuncElapsed("Navi_" + naviDomain, navQC.Elapsed));
                    }
                }
                catch
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 转换社区问答对
        /// </summary>
        /// <param name="faq_net"></param>
        /// <param name="coefficient"></param>
        /// <param name="qdc"></param>
        /// <param name="IDs"></param>
        private static void ConvetFAQ_NET(QueryContainer<CMFAQ_NET> faq_net, double coefficient, QueryDataContainer qdc, List<string> IDs)
        {
            if (faq_net != null)
            {
                if (faq_net.MetaList != null)
                {
                    foreach (var f in faq_net.MetaList)
                    {
                        CMBase cb = new CMBase();
                        var faqnet = new FaqNETAnswer() { Question = f.QUESTION, Answer = f.PREPARED_ANSWER, Time = f.TIME, UserName = f.USER_NAME, AnswerID = f.ANSWER_ID };
                        cb.Data = faqnet;
                        cb.QExp = faq_net.QExp;
                        cb.DataType = DataType.CMFAQ_NET;
                        string id = f.QUESTION.ToString().GetHashCode() + "_" + f.PREPARED_ANSWER.GetHashCode();
                        cb.ID = id;
                        cb.Weight = 0.95;
                        cb.OrderBy = coefficient * cb.Weight;
                        qdc.MetaList.Add(cb);
                        IDs.Add(id);
                    }
                    qdc.HaveAccurate = true;
                }
                if (!string.IsNullOrEmpty(faq_net.ErrorMsg))
                    qdc.ErrorMsg += string.Format("faq_net：{0};", faq_net.ErrorMsg);
                qdc.Elapsed += faq_net.Elapsed;
                qdc.ElapsedList.Add(new FuncElapsed("faq_net", faq_net.Elapsed));
            }
        }

        /// <summary>
        /// 转换领域问答对
        /// </summary>
        /// <param name="faq"></param>
        /// <param name="coefficient"></param>
        /// <param name="qdc"></param>
        /// <param name="IDs"></param>
        private static void ConvertFAQ(QueryContainer<CMFAQ> faq, double coefficient, QueryDataContainer qdc, List<string> IDs)
        {
            if (faq != null)
            {
                if (faq.MetaList != null)
                {
                    foreach (var f in faq.MetaList)
                    {
                        CMBase cb = new CMBase();
                        var faqAnswer = new FaqAnswer() { Domain = f.DOMAIN, Question = f.QUESTION, Answer = f.PREPARED_ANSWER };
                        if (f.ADDITIONAL_INFO != null)
                        {
                            faqAnswer.Extra = f.ADDITIONAL_INFO;
                        }
                        cb.Data = faqAnswer;
                        cb.QExp = faq.QExp;
                        cb.DataType = DataType.CMFAQ;
                        string id = f.QUESTION.ToString().GetHashCode() + "_" + f.PREPARED_ANSWER.GetHashCode();
                        cb.ID = id;
                        qdc.MetaList.Add(cb);
                        cb.Weight = 0.9;
                        cb.OrderBy = coefficient * cb.Weight;
                        IDs.Add(id);
                    }
                    qdc.HaveAccurate = true;
                }
                if (!string.IsNullOrEmpty(faq.ErrorMsg))
                    qdc.ErrorMsg += string.Format("faq：{0};", faq.ErrorMsg);
                qdc.Elapsed += faq.Elapsed;
                qdc.ElapsedList.Add(new FuncElapsed("faq", faq.Elapsed));

            }
        }
    }
}