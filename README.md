# DBotDDRefuge

`Discord` 機器人，基於 [Discord.Net](https://github.com/discord-net/Discord.Net)、[Victroia](https://github.com/Yucked/Victoria) 函式庫建構。

## 概述

因 `Discord` 的`訊息內容`取得權限將要轉為`特權意圖` [1]，之後需要通過認證，才可以存取使用者所發訊息的內容。  
  
故此機器人在設計時，直接不採用`文字指令`，而是使用`斜線命令`。  
  
[1] [Message Content: Privileged Intent for Verified Bots](https://support-dev.discord.com/hc/en-us/articles/4404772028055-Message-Content-Privileged-Intent-for-Verified-Bots)  
[2] [Message Content Intent: Review Policy](https://support.discord.com/hc/en-us/articles/4410940809111-Message-Content-Intent-Review-Policy)

## 斜線命令列表

- 一般類
  - `/ping`：取得`乓`回應。
  - `/time`：取得現在時間。
  - `/omikuji`：取得御神籤。
  - `/send-to`：傳送`網址連結`至指定的`文字頻道`。
- 日曆類
  - `/link`：取得日曆的連結。
  - `/calendar`：查詢日曆事件。
  - `/activity`：活動管理。
- 管理類
  - `/delete`：刪除訊息。
- 音樂類（需要配合 `Lavalink` 才能使用）
  - `/join`：加入語音頻道。
  - `/leave`：離開語音頻道。
  - `/play`：播放歌曲。
  - `/pause`：暫停播放。
  - `/resume`：恢復播放。
  - `/stop`：停止播放。
  - `/skip`：跳過目前歌曲。
  - `/seek`：更改目前歌曲的播放時間位置。
  - `/now-playing`：目前播放中的歌曲。
  - `/genius`：從 `Genuis` 取得歌詞。
  - `/ovh`：從 `OVH` 取得歌詞。
  - `/queue`：佇列清單。

## 設定檔（appsettings.json）

```json
{
  "discord": {
    "token": "{Discord 驗證 Token}",
    "totalShards": 1,
    "messageCacheSize": 1000
  },
  "dataSource": {
    "iCalendar": {
      "enable": true,
      "cacheMinutes": 10,
      "filterKeywords": "歌枠,歌回,歌,初配信,初配,新衣裝,新衣,周年,週年",
      "url": "https://calendar.google.com/calendar/ical/a4tve6el13u84ntjtvb1kvob10%40group.calendar.google.com/public/basic.ics",
      "embedUrl": "https://calendar.google.com/calendar/embed?src=a4tve6el13u84ntjtvb1kvob10%40group.calendar.google.com&ctz=Asia%2FTaipei"
    },
    "lavalink": {
      "enable": false,
      "hostname": "localhost",
      "port": 2333,
      "authorization": "{密碼}"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "System.Net.Http.HttpClient.*.LogicalHandler": "Warning",
      "System.Net.Http.HttpClient.*.ClientHandler": "Warning"
    }
  }
}
```
※請依自己的需求進行調整。

## Lavalink

- [GitHub - freyacodes/Lavalink: Standalone audio sending node based on Lavaplayer.](https://github.com/freyacodes/Lavalink)

## Windows 服務

### 建立服務
```
.\sc.exe create DBotDDRefuge binPath="{X}:\{路徑}\DBotDDRefuge.exe"
```
### 啟動服務
```
.\sc.exe start DBotDDRefuge
```
### 停止服務
```
.\sc.exe stop DBotDDRefuge
```
### 刪除服務
```
.\sc.exe delete DBotDDRefuge
```
※請使用**系統管理員**身分執行上述的指令。