using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Mvc;
using BaseService.ServiceCenter.SystemRuntime;
using CBase.DB;
using CService.QA.Collector;
using QA.Domain;
using QA.Domain.CMeta;
using QA.Domain.CModel;
using QA.API.Models;
using QA.web.Tool;
using Newtonsoft.Json;
using CBase.DB.Entity;
using MyQA.KB.Domain.CModel;
using QA.API.Filter;

namespace QA.API.Controllers
{
    //[System.Web.Http.RoutePrefix("api/Query")]
    [EnableCors("*", "*", "*")]
    public class QueryApiController : ApiController
    {
        public static int timeout = int.Parse(ConfigurationManager.AppSettings["timeout"]);

        /// <summary>
        /// 获取kb+faq
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet]
        [ValidateInput(false)]
        [System.Web.Http.Route("GetAnswer")]
        [BaseAuth]
        public async Task<Object> GetAnswer(string q, string appid, string aid)
        {
            try
            {
                CBase.Log.TimeWatchLog twTotal = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return new { result = false, msg = "问题不能为空!" };

                //根据意图分析结果，提供准确答案与资源导航、问答对
                Dictionary<string, QueryContainer<CMKArea>> navContent = null;
                QueryContainer<CMFAQ> faq = null;
                QueryContainer<CMFAQ_NET> faq_net = null;

                Task<Dictionary<string, QueryContainer<CMKArea>>> tNavi = null;
                List<Task<QueryContainer<CMFAQ>>> tFaqList = null;
                Task<QueryContainer<CMFAQ_NET>> tFaq_net = null;

                ClientInfo cinfo = new ClientInfo()
                {
                    UserName = CommonHelper.GetUserName(),
                    IP = CommonHelper.GetClientIP()
                };

                //获取意图识别               
                Domain.Intent intent = new Answer().GetIntent(q);

                //内容收集 意图是否识别收集用于数据分析 1未识别 2已识别
                ContentLogType collectType = ContentLogType.已识别;
                if (intent != null && intent.qt_parsed_rst == null)
                {
                    collectType = ContentLogType.未识别;
                }
                var client = new CollectorInterfaceClient();
                Task.Run<bool>(() =>
                {
                    return client.SetConfused(new CollectorEntity()
                    {
                        Content = intent.RawInput,
                        Type = (int)collectType,
                        IP = cinfo.IP,
                        UserID = cinfo.UserName
                    });
                });
                List<string> filterDomain = new List<string>();
                QueryConfig qconfig = new QueryConfig().GetQueryConfig();
                if (qconfig != null && qconfig.DomainMap.ContainsKey("default"))
                {
                    filterDomain = qconfig.DomainMap["default"].FAQ;
                }

                var tasks = new List<Task>();
                cinfo.Type = ContentLogType.知识库超时;
                tNavi = TimedTask.CallWithTimeoutAsync(timeout, () => new KB().Get(intent), q, cinfo);

                cinfo.Type = ContentLogType.问答集超时;
                tFaqList = GetFAQTask(q, timeout, filterDomain);

                cinfo.Type = ContentLogType.用户问答集超时;
                tFaq_net = TimedTask.CallWithTimeoutAsync(timeout, () => new Faq_net().Get(q), q, cinfo);
                tasks.Add(tNavi);

                tasks.AddRange(tFaqList);

                tasks.Add(tFaq_net);
                await Task.WhenAll(tasks.ToArray());
                navContent = tNavi == null ? null : tNavi.Result;

                faq = GetFAQAll(tFaqList);

                //如果faq和faqnet有内容就不返回知识库内容
                faq_net = tFaq_net == null ? null : tFaq_net.Result;
                if ((faq != null && faq.Total > 0) || (faq_net != null && faq_net.Total > 0))
                {
                    navContent = null;
                }

                cinfo.Type = ContentLogType.问题排序超时;
                QueryDataContainer SortData = DataSort.SortKB(faq, faq_net, navContent, cinfo);
                twTotal.Write("GetAnswer");
                if (SortData.MetaList.Count == 0)
                {
                    return new { result = false, msg = "未找到查询结果!" };
                }
                return new { result = true, SortData.MetaList };
            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }
        }

        [System.Web.Http.HttpGet]
        [ValidateInput(false)]
        [System.Web.Http.Route("GetKBDataByPage")]
        public Object GetKBDataByPage(string q, string domain, string intent_domain, string intent_id, string pageNum)
        {
            var emptyObject = new { result = false, msg = "未找到查询结果!" };
            IntentItem itentItem = null;
            Domain.Intent intent = new Answer(domain).GetIntent(q);
            if (intent.qt_parsed_rst == null)
                return null;

            foreach (var item in intent.qt_parsed_rst)
            {
                if (item.results == null)
                {
                    continue;
                }

                if (item.qt_domain == domain && item.results[0].domain == intent_domain && item.intent_no == intent_id)
                {
                    itentItem = item;
                    break;
                }
            }
            string intentItem = string.Empty;

            int pnum = 0;
            try
            {
                pnum = int.Parse(pageNum);
            }
            catch { return emptyObject; }

            //try
            //{
            //    itentItem = Newtonsoft.Json.JsonConvert.DeserializeObject<IntentItem>(intentItem);
            //}
            //catch (System.Exception ex)
            //{
            //    CBase.Log.Logger.Error(ex);
            //}
            if (itentItem == null)
                return emptyObject;

            itentItem.results[0].pagenum = pnum;

            CMKArea karea = null;
            var qc = new KB().GetItem(itentItem);
            if (qc != null && qc.Total > 0)
            {
                karea = qc.MetaList[0];
                karea.Page.PageNum = pnum;
                return new { result = true, karea };
            }
            else
            {
                return emptyObject;
            }

            //GetBookInfo(karea);




        }

        [System.Web.Http.HttpGet]
        [ValidateInput(false)]
        [System.Web.Http.Route("GetKBsql")]
        public Object GetKBsql(string q, string domain, string intentdomain)
        {
            try
            {
                if (string.IsNullOrEmpty(q))
                    return null;

                TestIntent testDal = new TestIntent();
                string buf = "";
                var relist = testDal.Query(domain, intentdomain, q, out buf);
                Intent intent = JsonConvert.DeserializeObject<Intent>(buf);
                //Intent intent = new Answer(domain).GetIntent(q, domain);

                if (intent == null || intent.qt_parsed_rst == null)
                {
                    return new { msg = "意图未识别" };
                }
                Dictionary<string, List<QexFilter>> kbsql = new KBCustSql().GetKsql(intent);
                Dictionary<string, List<dynamic>> data = new Dictionary<string, List<dynamic>>();
                foreach (var sql in kbsql)
                {
                    //单个结果集多个结果集sql
                    List<dynamic> ldy = new List<dynamic>();
                    foreach (var ex in sql.Value)
                    {
                        dynamic dy = new ExpandoObject();
                        dy.sql = ex.Qex.Render();
                        /*dy.order= ex.Qex.ExpModel.Order.Render();
                        dy.group = ex.Qex.ExpModel.Group.Render();
                        dy.top = ex.Qex.ExpModel.TOP;*/
                        if (ex.Filter != null && ex.Filter.Count > 0)
                        {
                            List<string> fsql = new List<string>();
                            foreach (var fex in ex.Filter)
                            {
                                fsql.Add(fex.ExpFilter.Render());
                            }
                            dy.filtersql = fsql;
                        }
                        ldy.Add(dy);
                    }
                    //sql 字典
                    data.Add(sql.Key, ldy);
                }

                if (data.Count == 0)
                {
                    return new { msg = "意图已识别，但没有生成sql" };
                }
                return new { data };
            }
            catch (Exception ex)
            {
                return new { msg = ex.ToString() };
            }
        }

        [System.Web.Http.HttpGet]
        [ValidateInput(false)]
        [System.Web.Http.Route("ClearCache")]
        public bool ClearCache()
        {
            try
            {
                CBase.Cache.CacheHelper.RemoveAllCache();
                return true;
            }
            catch (Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// 获取答案 通过专题领域配置
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet]
        [ValidateInput(false)]
        [System.Web.Http.Route("GetAnswerByTopic")]
        [BaseAuth]
        public async Task<Object> GetAnswerByTopic(string q, string topic, string appid, string aid)
        {
            try
            {
                CBase.Log.TimeWatchLog twTotal = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return new { result = false, msg = "参数不对!" };

                if (string.IsNullOrEmpty(topic))
                    return new { result = false, msg = "参数不对!" };

                //根据意图分析结果，提供准确答案与资源导航、问答对
                Dictionary<string, QueryContainer<CMKArea>> navContent = null;
                QueryContainer<CMFAQ> faq = null;
                QueryContainer<CMFAQ_NET> faq_net = null;

                Task<Dictionary<string, QueryContainer<CMKArea>>> tNavi = null;
                List<Task<QueryContainer<CMFAQ>>> tFaqList = null;
                //Task<QueryContainer<CMFAQ>> tFaq = null;
                Task<QueryContainer<CMFAQ_NET>> tFaq_net = null;

                ClientInfo cinfo = new ClientInfo()
                {
                    UserName = CommonHelper.GetUserName(),
                    IP = CommonHelper.GetClientIP()
                };

                //获取意图识别               
                Domain.Intent intent = new Answer(topic).GetIntent(q, topic);

                //内容收集 意图是否识别收集用于数据分析 1未识别 2已识别
                ContentLogType collectType = ContentLogType.已识别;
                if (intent != null && intent.qt_parsed_rst == null)
                {
                    collectType = ContentLogType.未识别;
                }
                var client = new CollectorInterfaceClient();
                Task.Run<bool>(() =>
                {
                    return client.SetConfused(new CollectorEntity()
                    {
                        Content = intent.RawInput,
                        Type = (int)collectType,
                        IP = cinfo.IP,
                        UserID = cinfo.UserName
                    });
                });

                List<string> FQA_fdomains = null;
                QueryConfig qconfig = new QueryConfig().GetQueryConfig();
                topic = topic.ToUpper();
                if (qconfig != null && qconfig.DomainMap.ContainsKey(topic))
                {
                    FQA_fdomains = qconfig.DomainMap[topic].FAQ;
                }
                else
                {
                    FQA_fdomains = new List<string>();
                    FQA_fdomains.Add(topic);
                }

                var tasks = new List<Task>();

                cinfo.Type = ContentLogType.知识库超时;
                tNavi = TimedTask.CallWithTimeoutAsync(timeout, () => new KB(topic).Get(intent), q, cinfo);

                tFaqList = GetFAQTask(q, timeout, FQA_fdomains);

                cinfo.Type = ContentLogType.用户问答集超时;
                tFaq_net = TimedTask.CallWithTimeoutAsync(timeout, () => new Faq_net(topic).Get(q, topic), q, cinfo);
                tasks.Add(tNavi);

                tasks.AddRange(tFaqList);
                tasks.Add(tFaq_net);
                await Task.WhenAll(tasks.ToArray());
                navContent = tNavi == null ? null : tNavi.Result;

                faq = GetFAQAll(tFaqList);
                faq_net = tFaq_net == null ? null : tFaq_net.Result;

                cinfo.Type = ContentLogType.问题排序超时;
                QueryDataContainer SortData = DataSort.SortKB(faq, faq_net, navContent, cinfo);
                twTotal.Write("GetAnswerByTopic");
                if (SortData.MetaList.Count == 0)
                {
                    return new { result = false, msg = "未找到查询结果!" };
                }
                return new { result = true, SortData.MetaList };


            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }
        }

        [System.Web.Http.HttpGet]
        [ValidateInput(false)]
        [System.Web.Http.Route("IsExistAnswer")]
        public async Task<ExistAnswer> IsExistAnswer(string q)
        {
            CBase.Log.TimeWatchLog tlogTotal = new CBase.Log.TimeWatchLog();
            ExistAnswer ea = new ExistAnswer();

            ea.result = false;
            ea.AnswerType = new List<string>();
            if (string.IsNullOrWhiteSpace(q))
            {
                ea.error = "问题参数为空，请传入问题参数";
                return ea;
            }

            List<Task<bool>> tFaqList = null;

            try
            {
                DomainMapUnit confine = GetConfigLimit("Library");

                Domain.Intent intent = new Answer().GetIntent(q);

                Task<bool> kb = null;
                List<Task<bool>> taskList = new List<Task<bool>>();
                Task<bool> tFaq_net = null;
                kb = Task.Run<bool>(() =>
                {
                    var isExistKB = new KB().IsExist(intent);
                    return isExistKB;
                });
                //kb = TimedTask.CallWithTimeoutAsync(timeout, () => new KB().IsExist(intent), null, null);
                List<string> filterDomain = new List<string>();
                if (confine != null && confine.FAQEX != null && confine.FAQEX.Count > 0)
                {
                    filterDomain = confine.FAQEX;
                }
                tFaqList = GetFAQTaskExist(q, timeout, filterDomain);

                //tFaq_net = TimedTask.CallWithTimeoutAsync(timeout, () => new Faq_net().IsExist(q), null, null);
                tFaq_net = Task.Run<bool>(() =>
                {
                    return new Faq_net().IsExist(q);
                });

                //Task<bool> sg = Task.Run<bool>(() =>
                //{
                //    int recStart = 0;
                //    int recCount = 0;
                //    InitPageParam(null, null, out recStart, out recCount);

                //    ClientInfo cinfo = new ClientInfo();
                //    if (confine == null || confine.SG == null || confine.SG.Count == 0)
                //    {
                //        QueryContainer<CMAnswer>  qcAnswer = TimedTask.CallActionWithTimeoutSync(timeout, () => new Answer().Get(q, recStart, recCount), q, cinfo);
                //        return qcAnswer != null && qcAnswer.MetaList != null && qcAnswer.MetaList.Count > 0;
                //    }
                //    else
                //    {
                //        foreach (string domain in confine.KB)
                //        {
                //            QueryContainer<CMAnswer> qcAnswer = TimedTask.CallActionWithTimeoutSync(timeout, () => new Answer(domain).Get(q, recStart, recCount,domain), q, cinfo);
                //            if(qcAnswer != null && qcAnswer.MetaList != null && qcAnswer.MetaList.Count > 0)
                //            {
                //                return true;
                //            }
                //        }
                //        return false;
                //    }

                //});

                //await Task.WhenAll(new Task<bool>[]{kb,faq,sg});
                //if (faq != null)
                //    taskList.Add(faq);

                taskList.AddRange(tFaqList);
                taskList.Add(tFaq_net);

                if (kb != null)
                    taskList.Add(kb);

                await Task.WhenAll(taskList);

                ea.TimeSpan = tlogTotal.Debug();
                tlogTotal.Write("IsExistAnswer");

                if (kb != null && kb.Result == true)
                {
                    ea.AnswerType.Add("kb");
                }
                if (GetFAQExist(tFaqList))
                {
                    ea.AnswerType.Add("faq");
                }
                if (tFaq_net != null && tFaq_net.Result == true)
                {
                    ea.AnswerType.Add("faqnet");
                }
                //记录有无答案日志
                try
                {
                    
                    ClientInfo cinfo = new ClientInfo()
                    {
                        UserName = CommonHelper.GetUserName(),
                        IP = CommonHelper.GetClientIP()
                    };
                    cinfo.Type = ea.AnswerType.Count == 0 ? ContentLogType.无答案 : ContentLogType.有答案;
                    CommonFunc.WriteContentLog(cinfo, q);
                    
                }
                catch (Exception ex)
                {
                    CBase.Log.Logger.Error(ex);
                }


                //if(sg.Result==true)
                //{
                //    ea.AnswerType.Add("sg");
                //}

                if (ea.AnswerType.Count > 0)
                {
                    ea.result = true;
                    return ea;
                }
                else
                {
                    ea.result = false;
                    return ea;
                }
            }
            catch (Exception e)
            {
                ea.error = "查询时出现错误:" + e.Message;
                return ea;
            }
        }

        [System.Web.Http.HttpGet]
        [ValidateInput(false)]
        [System.Web.Http.Route("GetRelaData")]
        public async Task<Object> GetRelaData(string q, string domain = null)
        {
            return await Task.Run<Object>(() =>
            {
                CBase.Log.TimeWatchLog tlogTotal = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return new { result = false, msg = "参数错误" };
                Domain.Intent intent = new Answer().GetIntent(q, domain);
                try
                {
                    var result = new KB().GetRela(intent);
                    tlogTotal.Write("GetRelaData");
                    if (result.Total == 0)
                        return new { result = false, msg = "未找到" };


                    return new { result = true, result.MetaList };
                }
                catch (System.Exception ex)
                {
                    CBase.Log.Logger.Error(ex);
                    return new { result = false, msg = ex.ToString() };
                }
            });

        }


        #region 暂停接口
        //[System.Web.Http.HttpGet]
        //[ValidateInput(false)]
        //[System.Web.Http.Route("GetResult")]
        private async Task<Object> GetResult(string sql)
        {
            return await Task.Run<Object>(() =>
            {
                if (string.IsNullOrEmpty(sql))
                    return null;
                try
                {
                    var result = new KB().GetSQLQC(sql);
                    if (result.Total == 0)
                        return null;
                    return new { result = true, result.MetaList };
                }
                catch (System.Exception ex)
                {
                    CBase.Log.Logger.Error(ex);
                    return new { result = false, msg = ex.ToString() };
                }
            });
        }

        //[System.Web.Http.HttpGet]
        //[ValidateInput(false)]
        //[System.Web.Http.Route("GetKBData")]
        private async Task<Object> GetKBData(string q, string domain = null)
        {
            try
            {

                List<FuncElapsed> listElaps = new List<FuncElapsed>();
                CBase.Log.TimeWatchLog tlogTotal = new CBase.Log.TimeWatchLog();
                CBase.Log.TimeWatchLog tlog_temp = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return null;
                //根据意图分析结果，提供准确答案与资源导航、问答对
                Dictionary<string, QueryContainer<CMKArea>> navContent = null;
                Task<Dictionary<string, QueryContainer<CMKArea>>> tNavi = null;
                ClientInfo cinfo = new ClientInfo()
                {
                    UserName = CommonHelper.GetUserName(),
                    IP = CommonHelper.GetClientIP()
                };
                //获取意图识别
                tlog_temp.Start();
                Domain.Intent intent = new Answer(domain).GetIntent(q, domain);
                listElaps.Add(new FuncElapsed("GetIntent", tlog_temp.Debug()));

                //内容收集 意图是否识别收集用于数据分析 1未识别 2已识别
                ContentLogType collectType = ContentLogType.已识别;
                if (intent != null && intent.qt_parsed_rst == null)
                {
                    collectType = ContentLogType.未识别;
                }
                var client = new CollectorInterfaceClient();
                Task.Run<bool>(() =>
                {
                    return client.SetConfused(new CollectorEntity()
                    {
                        Content = intent.RawInput,
                        Type = (int)collectType,
                        IP = cinfo.IP,
                        UserID = cinfo.UserName
                    });
                });

                cinfo.Type = ContentLogType.知识库超时;
                tNavi = TimedTask.CallWithTimeoutAsync(timeout, () => new KB(domain).Get(intent), q, cinfo);
                await Task.WhenAll(tNavi);
                navContent = tNavi == null ? null : tNavi.Result;
                tlog_temp.Start();
                cinfo.Type = ContentLogType.问题排序超时;
                QueryDataContainer SortData = DataSort.SortKB(null, null, navContent, cinfo);
                listElaps.Add(new FuncElapsed("SortKB", tlog_temp.Debug()));
                AnswerResultKB result = new Models.AnswerResultKB()
                {
                    intent = intent,
                    SortData = SortData
                };
                result.timeSpan = tlogTotal.Debug();
                SortData.ElapsedList.AddRange(listElaps);
                if (SortData.MetaList.Count == 0)
                {
                    return new { result = false, msg = "未找到查询结果!" };
                }
                for (int i = 0; i < SortData.MetaList.Count; i++)
                {
                    SortData.MetaList[i].QExp = null;
                }
                return new { result = true, SortData.MetaList };

            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }
        }
        /// <param name="q"></param>
        /// <param name="cp">第几页</param>
        /// <param name="c">页大小</param>
        /// <param name="navc"></param>
        /// <returns></returns>
        //[System.Web.Http.HttpGet]
        //[ValidateInput(false)]
        //[System.Web.Http.Route("GetSGData")]
        private Object GetSGData(string q, string domain = null, string cp = null, string c = null, string navc = null)
        {
            try
            {
                List<FuncElapsed> listElaps = new List<FuncElapsed>();
                CBase.Log.TimeWatchLog tlogTotal = new CBase.Log.TimeWatchLog();
                CBase.Log.TimeWatchLog tlog_temp = new CBase.Log.TimeWatchLog();

                if (string.IsNullOrEmpty(q))
                    return null;

                int recStart = 0;
                int recCount = 0;

                InitPageParam(cp, c, out recStart, out recCount);

                //调用句群接口，返回内容
                QueryContainer<CMAnswer> qcAnswer = null;

                ClientInfo cinfo = new ClientInfo()
                {
                    UserName = CommonHelper.GetUserName(),
                    IP = CommonHelper.GetClientIP()
                };
                tlog_temp.Start();
                cinfo.Type = ContentLogType.句群超时;
                qcAnswer = TimedTask.CallActionWithTimeoutSync(timeout, () => new Answer(domain).Get(q, recStart, recCount, domain), q, cinfo);
                listElaps.Add(new FuncElapsed("SenGroup", tlog_temp.Debug()));

                tlog_temp.Start();
                cinfo.Type = ContentLogType.问题排序超时;
                QueryDataContainer SortData = DataSort.SortSenGroup(qcAnswer, cinfo);
                listElaps.Add(new FuncElapsed("SortSG", tlog_temp.Debug()));

                AnswerResultSG result = new Models.AnswerResultSG()
                {
                    SortData = SortData
                };
                result.timeSpan = tlogTotal.Debug();
                SortData.ElapsedList.AddRange(listElaps);

                if (SortData.MetaList.Count == 0)
                {
                    return new { result = false, msg = "未找到查询结果!" };
                }
                return new { result = true, SortData.MetaList };
            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }

        }

        //[System.Web.Http.HttpGet]
        //[ValidateInput(false)]
        //[System.Web.Http.Route("GetIntent")]
        private Object GetIntent(string q, string domain = null)
        {
            try
            {
                List<FuncElapsed> listElaps = new List<FuncElapsed>();
                CBase.Log.TimeWatchLog tlog_temp = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return null;
                //获取意图识别
                tlog_temp.Start();
                Domain.Intent intent = new Answer(domain).GetIntent(q, domain);
                listElaps.Add(new FuncElapsed("GetIntent", tlog_temp.Debug()));
                return new { result = true, data = intent };
            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }

        }

        //[System.Web.Http.HttpGet]
        //[ValidateInput(false)]
        //[System.Web.Http.Route("GetFAQData")]
        private async Task<Object> GetFAQData(string q, string domain = null)
        {
            try
            {
                List<FuncElapsed> listElaps = new List<FuncElapsed>();
                CBase.Log.TimeWatchLog tlogTotal = new CBase.Log.TimeWatchLog();
                CBase.Log.TimeWatchLog tlog_temp = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return null;
                QueryContainer<CMFAQ> faq = null;
                QueryContainer<CMFAQ_NET> faq_net = null;
                List<Task<QueryContainer<CMFAQ>>> tFaqList = null;
                Task<QueryContainer<CMFAQ_NET>> tFaq_net = null;

                //获取意图识别
                tlog_temp.Start();
                var tasks = new List<Task>();
                tFaqList = GetFAQTask(q, timeout, domain);
                tFaq_net = TimedTask.CallWithTimeoutAsync(timeout, () => new Faq_net(domain).Get(q, domain), q, null);
                tasks.AddRange(tFaqList);
                tasks.Add(tFaq_net);
                await Task.WhenAll(tasks.ToArray());
                faq = GetFAQAll(tFaqList);
                faq_net = tFaq_net == null ? null : tFaq_net.Result;
                tlog_temp.Start();
                QueryDataContainer SortData = DataSort.SortKB(faq, faq_net, null, null);

                SortData.ElapsedList.AddRange(listElaps);
                if (SortData.MetaList.Count == 0)
                {
                    return new { result = false, msg = "未找到查询结果!" };
                }
                return new { result = true, SortData.MetaList };
            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }
        }

        //[System.Web.Http.HttpGet]
        //[ValidateInput(false)]
        //[System.Web.Http.Route("GetFAQNETData")]
        private async Task<Object> GetFAQNETData(string q, string domain = null)
        {
            try
            {
                CBase.Log.TimeWatchLog tlogTotal = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return null;

                QueryContainer<CMFAQ_NET> faq_net = null;
                Task<QueryContainer<CMFAQ_NET>> tFaq_net = null;
                var tasks = new List<Task>();
                tFaq_net = TimedTask.CallWithTimeoutAsync(timeout, () => new Faq_net(domain).Get(q, domain), q, null);
                tasks.Add(tFaq_net);
                await Task.WhenAll(tasks.ToArray());

                faq_net = tFaq_net == null ? null : tFaq_net.Result;
                //QueryDataContainer SortData = DataSort.SortKB(null, faq_net, null, null);

                if (faq_net == null || faq_net.MetaList.Count == 0)
                {
                    return new { result = false, msg = "未找到查询结果!" };
                }
                return new { result = true, faq_net.MetaList };
            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }
        }

        //搜索问题获取Faq答案的简易接口
        //[System.Web.Http.HttpGet]
        //[ValidateInput(false)]
        //[System.Web.Http.Route("FaqSlim")]
        private async Task<Object> FaqSlim(string q)
        {
            try
            {
                List<FuncElapsed> listElaps = new List<FuncElapsed>();
                CBase.Log.TimeWatchLog tlogTotal = new CBase.Log.TimeWatchLog();
                CBase.Log.TimeWatchLog tlog_temp = new CBase.Log.TimeWatchLog();
                if (string.IsNullOrEmpty(q))
                    return null;
                QueryContainer<CMFAQ> faq = null;
                List<Task<QueryContainer<CMFAQ>>> tFaqList = null;

                var tasks = new List<Task>();
                tFaqList = GetFAQTask(q, timeout);
                tasks.AddRange(tFaqList);
                await Task.WhenAll(tasks.ToArray());
                faq = GetFAQAll(tFaqList);
                if (faq.MetaList.Count == 0)
                {
                    return new { result = false, msg = "未找到查询结果!" };
                }
                else
                {
                    List<FaqAnswer> AnswerList = new List<FaqAnswer>();
                    foreach (CMFAQ f in faq.MetaList)
                    {
                        FAQ_ANSWER_CONFIG orm = new FAQ_ANSWER_CONFIG("KB");
                        QueryContainer<CMFAQ_ANSWER_CONFIG> config = orm.Get(f.DOMAIN);
                        if (config.MetaList.Count > 0)
                        {
                            FaqAnswer answer = GetFaqAnswer(config.MetaList[0], f);
                            if (answer == null)
                            {
                                answer = new FaqAnswer()
                                {
                                    Question = f.QUESTION,
                                    Answer = f.PREPARED_ANSWER,
                                    Domain = f.DOMAIN,
                                    Extra = f.ADDITIONAL_INFO
                                };
                            }
                            AnswerList.Add(answer);
                        }
                        else
                        {
                            FaqAnswer answer = new FaqAnswer()
                            {
                                Question = f.QUESTION,
                                Answer = f.PREPARED_ANSWER,
                                Domain = f.DOMAIN,
                                Extra = f.ADDITIONAL_INFO
                            };
                            AnswerList.Add(answer);
                        }
                    }
                    return new { result = true, AnswerList };
                }
            }
            catch (System.Exception ex)
            {
                CBase.Log.Logger.Error(ex);
                return new { result = false, msg = ex.ToString() };
            }
        }

        #endregion

        #region 辅助方法

        private static DomainMapUnit GetConfigLimit(string key)
        {
            QueryLimit qconfig = new QueryLimit().Get();
            DomainMapUnit confine = null;
            if (qconfig != null && qconfig.DomainMap.ContainsKey(key))
            {
                confine = qconfig.DomainMap[key];
            }
            return confine;
        }

        private List<Task<QueryContainer<CMFAQ>>> GetFAQTask(string q, int timeout, List<string> filterDomain)
        {
            Faq faq = new Faq();
            List<Task<QueryContainer<CMFAQ>>> taskList = new List<Task<QueryContainer<CMFAQ>>>();
            List<string> dList = faq.GetDomain();
            if (dList == null)
                return taskList;

            foreach (var domain in dList)
            {
                if (filterDomain.Contains(domain))
                    continue;
                taskList.Add(TimedTask.CallWithTimeoutAsync(timeout, () => new Faq().GetAll(q, domain), null, null));
            }
            return taskList;

        }

        private List<Task<bool>> GetFAQTaskExist(string q, int timeout, List<string> filterDomain)
        {
            Faq faq = new Faq();
            List<Task<bool>> taskList = new List<Task<bool>>();
            List<string> dList = faq.GetDomain();
            if (dList == null)
                return taskList;

            foreach (var domain in dList)
            {
                if (filterDomain.Contains(domain))
                    continue;
                taskList.Add(TimedTask.CallWithTimeoutAsync(timeout, () => new Faq().IsExist(q, domain), null, null));
            }
            return taskList;

        }

        /// <summary>
        /// 通过 Faq答案配置 和 Faq答案ID 获取 Faq答案
        /// </summary>
        /// <param name="faqAnswerConfig">Faq答案配置</param>
        /// <param name="faq">包含 Faq答案ID 和 答案领域 的结构</param>
        /// <returns>Faq答案</returns>
        protected FaqAnswer GetFaqAnswer(CMFAQ_ANSWER_CONFIG faqAnswerConfig, CMFAQ faq)
        {
            string config = faqAnswerConfig.CONFIG;
            FaqConfigObject configObject = new FaqConfigObject();
            JsonConvert.PopulateObject(config, configObject);
            string sql = string.Format(configObject.SQL, faq.PREPARED_ANSWER);
            QueryContainer<QEntity> sqlResult = CommonFunc.GetResult(sql);
            if (sqlResult.MetaList.Count > 0)
            {
                FaqAnswer answer = new FaqAnswer();
                answer.Question = faq.QUESTION;
                answer.Answer = sqlResult.MetaList[0].FieldValue[configObject.AnswerField].ToString();
                answer.Domain = faq.DOMAIN;
                sqlResult.MetaList[0].FieldValue.Remove(configObject.AnswerField);
                answer.Extra = sqlResult.MetaList[0].FieldValue;
                return answer;
            }
            else
            {
                return null;
            }
        }

        protected static List<Task<QueryContainer<CMFAQ>>> GetFAQTask(string q, int timeout, string domain = null)
        {
            Faq faq = new Faq();
            List<Task<QueryContainer<CMFAQ>>> taskList = new List<Task<QueryContainer<CMFAQ>>>();

            if (!string.IsNullOrEmpty(domain))
            {
                taskList.Add(TimedTask.CallWithTimeoutAsync(timeout, () => new Faq().Get(q, domain), null, null));
            }
            else
            {
                List<string> dList = faq.GetDomain();
                if (dList == null)
                    return taskList;
                DomainMapUnit confine = GetConfigLimit("Library");
                if (confine != null && confine.FAQEX != null && confine.FAQEX.Count > 0)
                {
                    dList = dList.Except(confine.FAQEX).ToList<string>();
                }
                foreach (var d in dList)
                {
                    taskList.Add(TimedTask.CallWithTimeoutAsync(timeout, () => new Faq().Get(q, d), null, null));
                }
            }
            return taskList;
        }

        protected static QueryContainer<CMFAQ> GetFAQAll(List<Task<QueryContainer<CMFAQ>>> tFaqList)
        {
            List<QueryContainer<CMFAQ>> faqList = new List<QueryContainer<CMFAQ>>();
            foreach (var t in tFaqList)
            {
                if (t != null && t.Result != null && t.Result.Total > 0)
                {
                    faqList.Add(t.Result);
                }
            }

            QueryContainer<CMFAQ> faqQC = new QueryContainer<CMFAQ>(true);
            foreach (var item in faqList)
            {
                faqQC.MetaList.AddRange(item.MetaList);
                faqQC.Total += item.Total;
            }

            return faqQC;
        }

        protected static bool GetFAQExist(List<Task<bool>> tFaqList)
        {
            foreach (var t in tFaqList)
            {
                if (t != null && t.Result != null && t.Result == true)
                {
                    return true;
                }
            }

            return false;
        }

        protected List<Task<QueryContainer<CMFAQ>>> GetFAQTaskList(string q, int timeout, List<string> filterDomain)
        {
            Faq faq = new Faq();
            List<Task<QueryContainer<CMFAQ>>> taskList = new List<Task<QueryContainer<CMFAQ>>>();
            List<string> dList = faq.GetDomain();
            if (dList == null)
                return taskList;

            foreach (var domain in dList)
            {
                if (filterDomain.Contains(domain))
                    continue;
                taskList.Add(TimedTask.CallWithTimeoutAsync(timeout, () => new Faq().Get(q, domain), null, null));
            }
            return taskList;


        }

        /// <summary>
        /// 初始化翻页参数
        /// </summary>
        /// <param name="cp">第页</param>
        /// <param name="c">页大小</param>
        /// <param name="recStart"></param>
        /// <param name="recCount"></param>
        protected static void InitPageParam(string cp, string c, out int recStart, out int recCount)
        {
            int curpn = 0;
            recStart = 0;
            recCount = 0;
            if (cp != null && c != null)
            {
                int.TryParse(cp, out curpn);


                int.TryParse(c, out recCount);
            }
            if (curpn <= 0)
                curpn = 1;

            if (curpn >= 50)
                curpn = 50;

            if (recCount <= 0)
                recCount = 30;

            if (recCount >= 50)
                recCount = 50;

            recStart = (curpn - 1) * recCount;
        }

        #endregion

    }
}
