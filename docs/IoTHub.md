# Azure IoT Hub を作成する
[https://docs.microsoft.com/ja-jp/azure/iot-hub/](https://docs.microsoft.com/ja-jp/azure/iot-hub/)を参考に、以下の手順で IoT Hub を作成する。   
1. リソースグループの作成 
2. IoT Hub の作成 
3. コンシューマーグループを作成

---
## 1. リソースグループの作成  
Azure ポータルで、
[https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups](https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups) を参照して、リソースグループを作成する。リソースグループ名は、各自の使い勝手の良い名前を決めて使うこと。ここでは、便宜上、"<i>EnvHoLGroup</i>" とする。  
リージョンは、好きな場所を選択してよいが、このハンズオンで作成する全てのリソースを同じリージョンに作成すること。

※ IoT Hubをはじめとするサービスは、Microsoft Azure では、<b>リソース</b> と呼ばれ、一つのソリューションは大抵の場合、複数のリソースで構成される。リソースは必ず一つのグループに属する。

---
## 2. IoT Hub の作成 
作成したリソースグループ内に、IoT Hub を作成する。 
Azure IoT Hub の名前は、インターネット上のURLの一部として使われるので、利用可能な名前を使うこと。 
リージョンは、リソースグループと同じ場所を選択すること。  
![Create IoT Hub](./images/iothub/1-create-iothub.png)


---
## コンシューマーグループを作成 
IoT Hub が受信したデバイスからの環境データは、Stream Analytics で処理を行う。Stream Analytics への入力用のコンシューマーグループを作成する。 
コンシューマーグループの名前は、"<b>sa</b>" とする。 
ポータルで作成した IoT Hub を開き、左側の"組込みのエンドポイント"を選択し、表示されたページで入力する。
![Create Consumer Group](./images/iothub/2-create-consumer-group.png)

