//app.js
//https://qa.cnki.net
//http://192.168.27.79
const qaurl ='http://192.168.27.79'
const voiceurl = 'http://192.168.27.79'

App({
  onLaunch: function () {
    //调用API从本地缓存中获取数据
    var logs = wx.getStorageSync('logs') || []
    logs.unshift(Date.now())
    wx.setStorageSync('logs', logs)
  },
  getUserInfo:function(cb){
    var that = this
    if(this.globalData.userInfo){
      typeof cb == "function" && cb(this.globalData.userInfo)
    }else{
      //调用登录接口
      // wx.login({
      //   success: function () {
      //     wx.getUserInfo({
      //       success: function (res) {
      //         that.globalData.userInfo = res.userInfo
      //         typeof cb == "function" && cb(that.globalData.userInfo)
      //       }
      //     })
      //   }
      // })
    }
  },
  onError:function(err){
    // wx.showToast({
    //   title: err,
    //   icon: 'loading',
    //   duration: 1000,
    //   mask: true
    // })
  },
  globalData:{
    userInfo:null,
    qaurl: qaurl,
    voiceurl:voiceurl
  }
})