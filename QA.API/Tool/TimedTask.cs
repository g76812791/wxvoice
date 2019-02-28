using CBase.DB;
using QA.Domain.CMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CService.QA.Feedback;
using CService.QA.Collector;
using QA.API.Models;
using QA.Domain;
using System.Threading;
using BaseService.ServiceCenter.SystemRuntime;

namespace QA.web.Tool
{
    /// <summary>
    /// 限时操作类
    /// </summary>
    public class TimedTask
    {  
        /// <summary>
        /// 用于同步限时操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="action"></param>
        /// <param name="actionKey"></param>
        /// <returns></returns>
        public static T CallActionWithTimeoutSync<T>(int timeoutMilliseconds, Func<T> action, string actionKey, ClientInfo cinfo)
        {          
            T local = default(T);        

            var thread = new Thread(() =>
            {                
                var client = new FeedbackServiceInterfaceClient();
                local = action();
            }); 

            thread.Start();
            if(!thread.Join(timeoutMilliseconds))         
            {
                if(cinfo != null)
                {
                    var client = new CollectorInterfaceClient();
                    Task.Run<bool>(() =>
                    {
                        //Task.Delay(5000).Wait();
                        return client.SetConfused(new CollectorEntity()
                        {
                            Content = actionKey,
                            Type = (int)cinfo.Type,
                            IP = cinfo.IP,
                            UserID = cinfo.UserName
                        });
                    });
                }
               
            }

            return local;

        }

        /// <summary>
        /// 用于异步限时操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="action"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static async Task<T> CallWithTimeoutAsync<T>(int timeoutMilliseconds, Func<T> action, string actionKey, ClientInfo cinfo)
        {
            T local = default(T);

            return await Task.Run(() =>
            {
                Thread threadToKill = null;
                Func<T> wrappedAction = () =>
                {
                    threadToKill = Thread.CurrentThread;
                    return action();
                };
                IAsyncResult result = wrappedAction.BeginInvoke(null, null);
                if (result.AsyncWaitHandle.WaitOne(timeoutMilliseconds))
                {
                    local = wrappedAction.EndInvoke(result);
                }
                else
                {
                    if (cinfo != null)
                    {
                        var client = new CollectorInterfaceClient();
                        Task.Run<bool>(() =>
                        {
                            return client.SetConfused(new CollectorEntity()
                            {
                                Content = actionKey,
                                Type = (int)cinfo.Type,
                                IP = cinfo.IP,
                                UserID = cinfo.UserName
                            });
                        });
                    }
                    if (threadToKill != null)
                    {
                        threadToKill.Abort();
                    }
                }
                return local;
            });
        }
    }
}