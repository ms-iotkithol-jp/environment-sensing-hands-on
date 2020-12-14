# Local IP Address 表示用ユーティリティモジュール  
テストやデバッグ時に、いちいち Raspberry Pi に HDMI モニターやマウス、キーボードをつなぐのは超面倒くさい。同一のローカルネットに接続された 開発用 PC から リモートシェルでつなぐにしても、DHCP 環境下の場合、IP アドレスは時々変わるので、最低限、HDMI モニターとマウスをつないで確認する必要がある。本モジュールは、Module Twins の Reported Properties で、起動時に確認したローカル IP アドレスを報告してくれるので、Azure Portal でその値を確認すれば、即、開発用 PC からのリモートシェル接続を可能にする。  
## 実行環境 
Raspberry Pi 3以降 

## Build 方法 
Raspberry Pi 3以降のシェル上で、以下を実行。  
```
$ sudo build -t <azure container registry url>/getipaddresspython:<version tag> -f Dockerfile.arm32v7 .
$ sudo build push <azure container registry url>/getipaddresspython:<version tag>
```
※ 他の Linux 系 OS 上で動作する Azure IoT Edge の場合は、Dockerfile.arm32v7 のベースイメージを適宜変えること。  

## 配置方法  
Azure IoT Edge Module として配置する場合の、生成オプションは以下の通り。 
```
{
  "HostConfig": {
    "NetworkMode": "host",
    "Privileged": true
  },
  "NetworkingConfig": {
    "EndpointsConfig": {
      "host": {}
    }
  }
}
```

## IP アドレスの表示  
配置が完了すると、Module Twins に以下の様に表示される、
```
    "reported": {
      "networkInfo": {
        "eth0": "10.168.207.134/24",
        "wlan0": "10.104.48.208/21"
      },
      "$metadata": {
```
- eth0 - 有線接続の IP アドレス
- wlan0 - Wi-Fi 接続の IP アドレス 

