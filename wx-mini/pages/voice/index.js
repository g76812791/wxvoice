const recorderManager = wx.getRecorderManager()

var plugin = requirePlugin("WechatSI")
let manager = plugin.getRecordRecognitionManager()

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
    showimg: false,
    startTime: 0

  },
  //事件处理函数
  //开始录音的时候
  start: function() {

    const options = {
      duration: 30000,
      lang: "zh_CN"
    }

    //开始录音
    manager.start(options);

    // var that=this
    // setTimeout(function() {
    //   //结束录音  
    //   that.stop()
    // }, time)

  },

  //停止录音
  mstop: function() {
    console.log('jinru mstop')
    manager.stop()
  },

  touchStart: function(e) {
    console.log('touchStart....')
    var that = this
    that.setData({
      tip: '',
      showimg: true,
      startTime: e.timeStamp
    })
    that.start();
  },

  touchEnd: function(e) {

    console.log('touchEnd....')
    var that = this;
    var t = e.timeStamp - that.data.startTime

    console.log("-t" + t)
    if (t < 800) {
      that.setData({
        showimg: false
      })
      wx.showToast({
        title: '识别中。。。',
        icon: 'loading',
        duration: 1000,
        mask: true
      })
      that.sleep(1200)
    }
    that.setData({
      showimg: false
    })
    that.mstop()
  },

  //授权回调
  setcallback: function() {
    var that = this
    that.setData({
      tip: '说出你要查询的内容',
      showOrHidden: false
    })
  },

  //初始化插件
  initRecord: function() {
    // 识别结束事件
    manager.onStop = (res) => {
      var that = this
      console.log(res.duration)
      console.log("record file path", res.tempFilePath)
      console.log("result", res.result.replace(/[，|。]/gm, ''))

      if (res.result == "") {
        wx.showToast({
          title: '没有听到有效声音',
          icon: 'loading',
          duration: 1000,
          mask: true
        })
      }

      // wx.showToast({
      //   title: res.result.replace(/[，|。]/gm, ''),
      //   icon: 'loading',
      //   duration: 1000,
      //   mask: true
      // })
      else {
        wx.navigateTo({
          url: '../qaquery/index?q=' + res.result.replace(/[，|。]/gm, '')
        })
      }


    }

    // 识别错误事件
    manager.onError = (res) => {
      wx.hideToast()
      console.error("stop error msg", res.msg)
      // that.setData({
      //   tip: '声音无效,请重试'
      // })
      // that.setData({
      //   tip: '没有听到任何有效声音'
      // })
    }
  },

  sleep: function(n) {
    console.log('woshi seleep')
    var now = new Date()
    console.log(now.getTime())
    var exittime = now.getTime() + n;
    while (true) {
      now = new Date()
      if (now.getTime() > exittime) {
        console.log(exittime)
        return;
      }
    }
  },


  onLoad: function() {
    var that = this
    this.initRecord()
    wx.getSetting({
      success(res) {
        if (res.authSetting['scope.record']) {
          //do
        } else {

          wx.authorize({
            scope: 'scope.record',
            success() {
              //that.start();
              
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