# Azure IoT Edge を使ったデバイスの接続  
このステップでは、Azure IoT Edge を使って、デバイスを Azure IoT Hub に接続する。 

![architecture](../images/device/iot-edge.png) 

利用者の利便性を考え、簡易版と中級版を提供する。適宜選択して実習すること。  
簡易版、詳細版、いずれも、計測、テレメトリーデータ送信、設定情報更新等のモジュール本体は、[BarometerSensing モジュール](../EdgeSolution/modules/BarometerSensing) を使用する。

---
## IoT Edge デバイスの登録と Raspberry Pi の設定更新 
簡易版、中級版共に、まずは、Azure IoT Hub に IoT Edge デバイスの登録と、Raspberry Pi 上で動作する IoT Edge Runtime の設定を行う。

### IoT Edge デバイスの登録  
Azure ポータルで、'IoT Edge'を選択し、'+IoT Edge デバイスを追加する'をクリックし、IoT Edge名を入力して登録する。
![add](../images/edge-module/add-new-device.png)
IoT Edge 名は任意であるが、ここでは便宜上、'<i>env-edge-device-1</i>'とする。  
登録が完了したら、一覧に表示されるので、登録した IoT Edge デバイスをリスト上で選択する。  
![connection string](../images/edge-module/get-edge-policy.png)
Azure IoT Device SDK を使用したデバイス接続の時と同様に、ここで表示された接続文字列を、IoT Edge 利用時の接続文字列として使用する。

## Raspberry Pi 上の設定更新 IoT Edge Runtime
前ステップで取得した接続文字列を使って、Raspberry Pi 上の、IoT Edge Runtime の設定を更新する。  
Raspberry Pi 上の Shell で、以下を実行し、設定ファイルを編集する。
```
$ sudo vi /etc/iotedge/config.yml
```
config.yml の以下のパート、
```yaml
# Manual provisioning configuration
provisioning:
  source: "manual"
  device_connection_string: "<ADD DEVICE CONNECTION STRING HERE>"
```
の、<i>\<ADD DEVICE CONNECTION STRING HERE\></i>を、登録した IoT Edge デバイスの接続文字列に置き換える。  
編集が終わったら、ESCキーをクリックし、"wq!" と入力し、保存する。  
config.yml の更新が完了後に、以下のコマンドを Shell 上で実行する。  
```
$ sudo systemctl restart iotedge
```
このコマンド実行により、IoT Edge Runtime が新しい設定で起動され、Azure IoT Hub に接続され、データの送受信やロジック構成更新が可能になる。 

以上で、準備が完了する。  

---
## 簡易版 
Azure ポータルを利用して、Docker Hub で公開された BarometerSensing を IoT Edge デバイスに配置する。 

1. BarometerSensing モジュールの登録
2. モジュール配置の確認

### BarometerSeinsing モジュールの登録
接続文字列を取得したページを開き、'モジュールの設定'をクリックする。  
'+Add' をクリックして、'+ IoT Edge Module' を選択する。
![bigen-add](../images/edge-module/begin-add-module.png)  
Edge Runtime 上に配置する際のモジュール名と、Azure IoT Edge Module Image の URI を入力する。  
![set-module-name-uri](../images/edge-module/set-edge-module-uri.png)

- 'IoT Edge Module Name' → <b>BarometerSensing</b>  
- 'Image URI' → <b>embeddedgeorge/barometersensing:0.0.1-arm32v7</b>  

次に、'Container Create Options' を選択し、
![set-create-option](../images/edge-module/set-module-create-option.png)
`Value all options' に、以下のJSON分を転記し、
```JSON
{
  "HostConfig": {
    "Binds": [
      "/dev/i2c-1:/dev/i2c-1"
    ],
    "Privileged": true,
    "Devices": [
      {
        "PathOnHost": "/dev/i2c-1",
        "PathInContainer": "/dev/i2c-1",
        "CgroupPermissions": "mrw"
      }
    ]
  }
}
```
'Add' をクリックする。 
※ IoT Edge Module は、Docker Container である。上の設定は、マイクロサービス（一種の仮想環境）である Docker Container 内から BME280 センサー（I2Cデバイス） にアクセスするための設定である。  
この設定で、IoT Edge デバイスへの一つの IoT Edge Module の配置指定が完了する。  
次に、追加した、 BarometerSensing モジュールのテレメトリー情報を Azure IoT Hub に送信するための、メッセージルートを定義する。'Next Routes >' をクリックし、ルートの名前と、定義を入力する。  
![set-msg-route](../images/edge-module/set-message-route.png)
- 'NAME' → <b>BarometerSensingToIoTHub</B>
- 'VALUE' → <b>FROM /messages/modules/BarometerSensing/outputs/* INTO $upstream</b>  

入力が終わったら、'Next Review + create >' をクリックし、更に、'create' をクリックして、IoT Edge Modules の配置設定を完了する。  
![create-deployment](../images/edge-module/create-module-deployment.png)
以上のステップを完了すると、IoT Edge デバイスの表示に BarometerSensing モジュールが表示される。
![listed-modules](../images/edge-module/listed-module.png)
Raspberry Pi 上の IoT Edge Runtime に通知が行き、IoTEdge Runtime が指定の Uri から Docker Image を Pull して配置し、BarometerSensing モジュールの実行が開始され、IoT Hub に定期的にテレメトリー情報が送信される。  

---
### モジュール配置の確認 
Raspberry Pi の Shell 上で、以下のコマンドを実行し、配置したモジュールが表示されるか確認する。  
```
$ sudo iotedge list
```

IoT Device App の実習で紹介した、Azure IoT Explorer を使って、メッセージが Azure IoT Hub に送信されているか確認する。  

IoT Device App の実習で紹介した、デバイスツイン（IoT Edge Module の場合は、モジュールツイン)、サービス側からのメッセージ受信、ダイレクトメソッドのコールが、BarometerSensing モジュールには実装されている。「[Microsoft Docs の関連ページ](https://docs.microsoft.com/ja-jp/azure/iot-edge/iot-edge-modules)」を参照して、それぞれの機能を試していただきたい。  


---
中級版  
BarometerSensing は、「[チュートリアル:Linux デバイス用の C# IoT Edge モジュールを開発する](https://docs.microsoft.com/ja-jp/azure/iot-edge/tutorial-csharp-module)」 をベースに作成したモジュールを改造したものである。子のチュートリアルに従って、VS Code で、IoT Edge Solution を作成し、[device/EdgeSolution](../device/EdgeSolution)の中身と見比べて、どこが違うか確認し、チュートリアルに従って作成した、Edge Solution を改造し、このハンズオンで公開されているファイル群を使って、同じものを作成してみよう。  
ただし、Raspberry Pi 用の ARM32V7 用のモジュールの Build は、Windows PC 上ではできないので、[device/EdgeSolution/modules/BarometerSensing](../device/EdgeSolution/modules/BarometerSensig)の中身を、Raspberry Pi 上に転送し、Docker build、Docker tag、Docker push を使って、ビルドとタグ付けと、ACR へのプッシュを行う必要がある。このような状況は、実際の IoT ソリューション開発では日常的なことなので、各自調べてスキルアップを図ってほしい。  

---
[次のステップに進む](StreamAnalytics.md)