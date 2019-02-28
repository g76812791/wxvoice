const app = getApp();
const qaurl = app.globalData.qaurl;
Page({
  data: {
    t: '',
    qaurl, qaurl
  },
  onLoad: function (options) {
    this.setData({
      t: Math.random(),
      qaurl: qaurl
    })
  },
})
