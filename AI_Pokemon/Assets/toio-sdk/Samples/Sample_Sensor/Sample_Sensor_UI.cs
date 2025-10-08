//using UnityEngine;


//namespace toio.Samples.Sample_Sensor
//{
//    public class Sample_UI : MonoBehaviour
//    {
//        private int updateCnt = 0;

//        void Update()
//        {
//            if (CubeScanner.actualTypeOfAuto == ConnectType.Simulator)
//            {
//                if (updateCnt < 3) updateCnt++;
//                if (updateCnt == 2){
//                    // ステージを右に移動
//                    var localPos = Camera.main.transform.localPosition;
//                    localPos.x = -0.15f;
//                    Camera.main.transform.localPosition = localPos;
//                    // キャンバスを左に移動
//                    var canvasObj = GameObject.Find("Canvas");
//                    var simCanvasObj = GameObject.Find("SimCanvas");

//                    canvasObj.transform.SetParent(simCanvasObj.transform);
//                    canvasObj.transform.position = new Vector3(860/2 * canvasObj.transform.localScale.x * 0.8f,
//                        canvasObj.transform.position.y, canvasObj.transform.position.z);
//                }

//            }
//        }
//    }
//}


using UnityEngine;
using System.Collections;

namespace toio.Samples.Sample_Sensor
{
    public class Sample_UI : MonoBehaviour
    {
        //    private int updateCnt = 0;
        //    private GameObject canvasObj;
        //    private GameObject simCanvasObj;

        //    void Start()
        //    {
        //        // 确保在开始时就查找对象
        //        canvasObj = GameObject.Find("Canvas");
        //        simCanvasObj = GameObject.Find("SimCanvas");

        //        // 设置相机初始位置
        //        if (CubeScanner.actualTypeOfAuto == ConnectType.Simulator)
        //        {
        //            var localPos = Camera.main.transform.localPosition;
        //            localPos.x = -0.15f;
        //            Camera.main.transform.localPosition = localPos;
        //        }

        //        // 开始协程，等待一段时间后执行移动操作
        //        StartCoroutine(DelayedAction());
        //    }

        //    IEnumerator DelayedAction()
        //    {
        //        // 等待一段时间，确保资源加载完成
        //        yield return new WaitForSeconds(1f);

        //        if (CubeScanner.actualTypeOfAuto == ConnectType.Simulator)
        //        {
        //            if (updateCnt < 3) updateCnt++;
        //            if (updateCnt == 2)
        //            {
        //                if (canvasObj != null && simCanvasObj != null)
        //                {
        //                    // キャンバスを左に移動
        //                    canvasObj.transform.SetParent(simCanvasObj.transform);
        //                    canvasObj.transform.position = new Vector3(860 / 2 * canvasObj.transform.localScale.x * 0.8f,
        //                        canvasObj.transform.position.y, canvasObj.transform.position.z);
        //                }
        //            }
        //        }
        //    }

        //    void Update()
        //    {
        //        if (CubeScanner.actualTypeOfAuto == ConnectType.Simulator)
        //        {
        //            if (updateCnt < 3) updateCnt++;
        //        }
        //    }
    }
}