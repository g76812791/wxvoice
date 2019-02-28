const recorderManager = wx.getRecorderManager()
const app = getApp();
const time = 10000;
const voiceurl = app.globalData.voiceurl;
Page({
  data: {
    voiceButtonName: '长按录音',
    voiceButtonDisable: false,
    tempFilePath: null,
    tip: '说出你要查询的内容',
    showOrHidden: false, //true表示显示，false隐藏
    showimg:false
  },
  //事件处理函数
  //开始录音的时候
  start: function() {
    const options = {
      duration: time, //指定录音的时长，单位 ms
      sampleRate: 16000, //采样率
      numberOfChannels: 1, //录音通道数
      encodeBitRate: 96000, //编码码率
      format: 'mp3', //音频格式，有效值 aac/mp3
      frameSize: 50, //指定帧大小，单位 KB
    }
    //开始录音
    recorderManager.start(options);
    recorderManager.onStart(() => {
      console.log('recorder start')
    });
    //错误回调
    recorderManager.onError((res) => {
      console.log(res);
    })

    // var that=this
    // setTimeout(function() {
    //   //结束录音  
    //   that.stop()
    // }, time)

  },

  //停止录音
  stop: function() {
    recorderManager.stop();
    recorderManager.onStop((res) => {
      var that = this
      console.log(res.duration)
      if (res.duration < 100) {
        that.setData({
          tip: '录音时间太短'
        })
        return;
      }

      if (res.duration > 60000) {
        that.setData({
          tip: '录音时间不能超过1分钟'
        })
        return;
      }

      console.log('声音文件名', res.tempFilePath)
      wx.showToast({
        title: '语音识别中',
        icon: 'loading',
        duration: 5000,
        mask: true
      })

      wx.uploadFile({
        url: voiceurl + '/voice/api/GetText',
        filePath: res.tempFilePath,
        name: 'file',
        //header: {},
        formData: {
        },
        success: function(res) {
          console.log(res.data);
          var json = JSON.parse(res.data);
          console.log(json)
          console.log(json.err_no)
          if (json.err_msg == "Success") {
            wx.navigateTo({
              url: '../qaquery/index?q=' + json.result.word.join('')
            })
          } else if (json.err_msg == "Error") {
            that.setData({
              tip: '声音无效,请重试'
            })
          } else if (json.err_no == '3301') {
            that.setData({
              tip: '没有听到任何有效声音'
            })
          }
          wx.hideToast()
        },
        fail: function(err) {
          that.setData({
            tip: '声音无效,请重试'
          })
          console.log('发送声音文件失败', err)
          wx.hideToast()
        },
         complete: function() {
          console.log('发送完成')
        }
      })
    })
  },

  touchStart: function() {
    console.log('touchStart....')

    var that = this
    that.setData({
      tip: '',
      showimg:true
    })
    that.start();
  },

  touchEnd: function() {
    console.log('touchEnd....')
   
    var that = this;
    that.setData({
      showimg: false
    })
    that.stop()
  },
  setcallback:function(){
    var that = this
    that.setData({
      tip: '说出你要查询的内容',
      showOrHidden: false
    })
  },
  onLoad: function() {
    var that = this;
    wx.getSetting({
      success(res) {
        if (res.authSetting['scope.record']) {
          //do
        } else {

          wx.authorize({
            scope: 'scope.record',
            success() {
              //do
            },
            fail: function(err) {
              that.setData({
                tip: '未授权语音权限，打开授权',
                showOrHidden: true
              })
            }
          })

        }
      }
    })
  }
})

