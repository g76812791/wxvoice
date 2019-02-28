using System;
using System.Configuration;
using System.Threading;
using BaseService.ServiceCenter.Redis;
using RedisNetCommandListener = BaseService.ServiceCenter.Redis.RedisNetCommandListener;

namespace CService.QA.Custom
{
    public class SubscribePublish : IDisposable
    {
        private RedisNetCommandListener redislistener = null;
        private CancellationTokenSource cancelSource;
        //private RedisNetCommand redisCommand = null;
        private string _redisServer;
        public SubscribePublish()
        {
            _redisServer = ConfigurationManager.AppSettings["servernode"];
        }

        public void Subscribe(string channelname, Action<string> action)
        {
            cancelSource = new CancellationTokenSource();
            redislistener = new RedisNetCommandListener(_redisServer);
            redislistener.Name = "客户端" + GetType();
            redislistener.Register(
            (channel, msg) =>
            {
                if (!cancelSource.IsCancellationRequested)
                {
                    action(msg);
                }
            }            , cancelSource

            , channelname);
        }

        public void Publish( string channelname, string msg)
        {
            RedisNetCommand redisNetCommand = new RedisNetCommand(_redisServer);
            redisNetCommand.SendMessage(channelname, msg);
        }

        public void Dispose()
        {
            if (redislistener != null)
                redislistener.Dispose();
            if (cancelSource != null)
                cancelSource.Cancel();
        }


    }
}
