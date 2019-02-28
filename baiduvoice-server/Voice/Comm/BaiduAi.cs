using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using Baidu.Aip.Speech;
using NAudio.Wave;
namespace Voice.Comm
{
    public class BaiduAi
    {

        private static Asr _instance = null;
        private static readonly object SynObject = new object();
        BaiduAi()
        {
        }

        public static Asr Instance
        {
            get
            {
                if (null == _instance)
                {
                    lock (SynObject)
                    {
                        if (null == _instance)
                        {
                            var APP_ID = ConfigurationManager.AppSettings["APP_ID"];
                            var API_KEY = ConfigurationManager.AppSettings["API_KEY"];
                            var SECRET_KEY = ConfigurationManager.AppSettings["SECRET_KEY"];
                            _instance = new Asr(APP_ID, API_KEY, SECRET_KEY);
                            _instance.Timeout = 60000;  // 修改超时时间
                        }
                    }
                }
                return _instance;
            }
        }

        public static string GetWavPath(string filePath)
        {
            string newFolder = System.AppDomain.CurrentDomain.BaseDirectory + "NewSoundFiles/";
            string newFilePath = newFolder + Path.GetFileNameWithoutExtension(filePath) + "-new.wav";
            try
            {
                if (filePath.EndsWith(".mp3", StringComparison.CurrentCultureIgnoreCase))
                {
                    using (Mp3FileReader reader = new Mp3FileReader(filePath))
                    {
                        var newFormat = new WaveFormat(16000, 16, 1); // 16k
                        using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                        {
                            WaveFileWriter.CreateWaveFile(newFilePath, conversionStream);
                        }
                    }
                }
                else
                {
                    using (AudioFileReader reader = new AudioFileReader(filePath))
                    {
                        var newFormat = new WaveFormat(16000, 16, 1); // 16k
                        using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                        {
                            WaveFileWriter.CreateWaveFile(newFilePath, conversionStream);
                        }
                    }
                }
            }
            catch
            {
            }
            return newFilePath;
        }
        // WaveStream pcm = BaiduAi.GetWavStream(@"D:\voice.mp3");
        public static WaveStream GetWavStream(string filePath)
        {
            Mp3FileReader reader = new Mp3FileReader(filePath);
            return WaveFormatConversionStream.CreatePcmStream(reader);
        }
    }
}