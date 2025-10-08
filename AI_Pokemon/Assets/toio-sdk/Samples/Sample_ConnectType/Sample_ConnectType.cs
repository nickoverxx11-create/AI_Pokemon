//using UnityEngine;


//namespace toio.Samples.Sample_ConnectName
//{
//    public class Sample_ConnectType : MonoBehaviour
//    {
//        public ConnectType connectType;

//        CubeManager cm;
//        async void Start()
//        {
//            // ConnectType.Auto - ビルド対象に応じて内部実装が自動的に変わる
//            // ConnectType.Simulator - ビルド対象に関わらずシミュレータのキューブで動作する
//            // ConnectType.Real - ビルド対象に関わらずリアル(現実)のキューブで動作する
//            cm = new CubeManager(connectType);
//            await cm.MultiConnect(2);
//        }


//        void Update()
//        {
//            foreach(var cube in cm.syncCubes)
//            {
//                cube.Move(50, -50, 100);
//            }
//        }
//    }
//}

using UnityEngine;
using toio.Samples.Sample_Sensor;
using static toio.CubeOrderBalancer;
using Cysharp.Threading.Tasks;

namespace toio.Samples.Sample_ConnectName
{
    public class Sample_ConnectType : MonoBehaviour
    {
        public ConnectType connectType;

        public CubeManager cm;

        public Cube cubeData;
        
        //async void Start()
        //{
            // ConnectType.Auto（自动连接类型） - 会根据构建目标，内部实现自动发生变化。
            // ConnectType.Simulator（模拟器连接类型） - 无论构建目标是什么，都在模拟器的立方体上运行。
            // ConnectType.Real（真实连接类型） - 无论构建目标是什么，都在真实（现实）的立方体上运行。
            //cm = new CubeManager(connectType);
            //await cm.MultiConnect(2);

            //cubeData = cm.syncCubes[0];
        //}

        public async void OnBtnConnect() { await Connect(); }
        private async UniTask Connect()
        {
            // Cube の接続
            var peripheral = await new CubeScanner(connectType).NearestScan();
            cubeData = await new CubeConnecter(connectType).Connect(peripheral);

            // モーター速度の読み取りをオンにする
            await cubeData.ConfigMotorRead(true);

            //cubeData.connectionIntervalCallback.AddListener("Sample_Sensor", OnConnectionInterval);  // Connection Interval

            await cubeData.ConfigIDNotification(500);       // 精度10ms
            await cubeData.ConfigIDMissedNotification(500); // 精度10ms
            //await cubeData.ConfigConnectionInterval(100, 200, timeOutSec: 2f, callback: OnConfigConnectionInterval); // 125ms ~ 250ms
            await UniTask.Delay(500);
            cubeData.ObtainConnectionInterval();
        }

        void Update()
        {
            //if (Input.GetKeyDown(KeyCode.A))
            //{
            //    foreach (var cube in cm.syncCubes)
            //    {
            //        cube?.Move(-45, 45, durationMs: 0, order: Cube.ORDER_TYPE.Strong);
            //    }
            //}
            //if (Input.GetKeyDown(KeyCode.D))
            //{
            //    foreach (var cube in cm.syncCubes)
            //    {
            //        cube?.Move(45, -45, durationMs: 0, order: Cube.ORDER_TYPE.Strong);
            //    }
            //    //cube?.Move(45, -45, durationMs: 0, order: Cube.ORDER_TYPE.Strong);
            //}
            //if (Input.GetKeyDown(KeyCode.E))
            //{
            //    foreach (var cube in cm.syncCubes)
            //    {
            //        Debug.Log(cube.id);
            //        Debug.Log(cube.addr);
            //        Debug.Log(cube.localName);
            //    }
            //}
            //foreach (var cube in cm.syncCubes)
            //{
            //    cube?.Move(45, -45, durationMs: 0, order: Cube.ORDER_TYPE.Strong);
            //}
            
        }
    }

    //public void OnConfigConnectionInterval(bool success, Cube c)
    //{
    //    Debug.Log("Config Connection Interval success: " + success.ToString());
    //}

    //public void OnConnectionInterval(Cube c)
    //{
    //    Debug.Log("Current Connection Interval: " + (c.connectionInterval * 1.25f).ToString() + "ms");
    //}
}
