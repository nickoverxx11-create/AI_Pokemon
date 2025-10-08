using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


namespace toio.Samples.Sample_ConnectName
{
    public class Sample_ConnectName_UI : MonoBehaviour
    {
        public ConnectType connectType = ConnectType.Real;
        public GameObject cubeItemPrefab;
        public RectTransform panelConnect;
        public RectTransform listContent;
        public Button buttonConnect;
        public Text textStatus;

        CubeScanner scanner;
        CubeConnecter connecter;
        Cube cube;
        Dictionary<string, GameObject> cubeItems = new Dictionary<string, GameObject>();

        public bool connected => cube != null && cube.isConnected;

        void Start()
        {
            this.scanner = new CubeScanner(this.connectType);
            this.connecter = new CubeConnecter(this.connectType);

            Debug.Log("This app is for " + (this.scanner.actualType == ConnectType.Real ? "real" : "simulator") + " cubes.");
        }

        public async void OnBtnConnect()
        {
            if (!connected) {
                panelConnect.gameObject.SetActive(true);
                this.scanner.StartScan(OnScan).Forget();
            } else {
                this.connecter.Disconnect(cube);
                this.cube = null;
                await UniTask.Delay(100);
                this.buttonConnect.GetComponentInChildren<Text>().text = "Connect";
            }
        }
        public void OnBtnCancel () {
            this.scanner.StopScan();
            panelConnect.gameObject.SetActive(false);
            // Clear list
            foreach (var addr in this.cubeItems.Keys.ToArray())
            {
                Destroy(this.cubeItems[addr]);
                this.cubeItems.Remove(addr);
            }
        }
        void OnScan(BLEPeripheralInterface[] peris) {
            if (peris.Length == 0) return;
            foreach (var peri in peris) {
                if (peri == null) continue;
                if (this.cubeItems.ContainsKey(peri.device_address)) continue;
                peri.AddConnectionListener("Sample_ConnectName", this.OnConnection);

                // Create list item
                var item = Instantiate(this.cubeItemPrefab, this.listContent);
                item.GetComponent<Button>().onClick.AddListener(async () => await OnItemClick(peri));
                var name = peri.device_name;
                if (name.Length == 0) name = "Unknown";
                item.GetComponentInChildren<Text>().text = name;
                this.cubeItems.Add(peri.device_address, item);
            }
        }

        async UniTask OnItemClick(BLEPeripheralInterface peripheral)
        {
            try {
                this.cube = await this.connecter.Connect(peripheral);
                this.OnBtnCancel();
                this.textStatus.text = this.cube.localName +  " connected";

                if (this.cube == null)
                    throw new System.Exception("Connection Timeout.");
                this.buttonConnect.GetComponentInChildren<Text>().text = "Disconnect";
            }
            catch (System.Exception e) {
                Debug.LogError(e);
            }
        }

        void OnConnection(BLEPeripheralInterface peri) {
            if (!peri.isConnected) {
                if (!this.textStatus.IsDestroyed())
                    this.textStatus.text = "";
            }
        }

        // 将持续时间（durationMs）设置为 0 就会变为无时间限制，这样只需调用一次，它就可以持续运行。
        // 将命令的优先级（order）设置为 Cube.ORDER_TYPE.Strong，就能安全地发送一次性命令。
        // 【详细说明】：
        // 当把命令的优先级设置为 Strong 时，内部会将命令添加到命令队列中，并在可执行命令的帧时刻依次发送命令，就是这样一种机制。
        // 通常情况下，在发送命令前会调用 cubeManager.IsControllable (cube) 来确认是否处于可执行命令的帧，但在本次操作中，因为已经将命令的优先级设置为了 Strong，所以无需调用 cubeManager.IsControllable (cube)，直接将命令添加到命令队列即可。
        // ※ 顺便一提，如果调用了 cubeManager.IsControllable (cube) 来事先确认可执行命令的帧，那么就会出现命令丢失，而不是数据包丢失的情况。
        public void Forward() { cube?.Move(60, 60, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
        public void Backward() { cube?.Move(-40, -40, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
        public void TurnRight() { cube?.Move(60, 30, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
        public void TurnLeft() { cube?.Move(30, 60, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
        public void Stop() { cube?.Move(0, 0, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
        public void PlayPresetSound1() { cube?.PlayPresetSound(1); }
        public void PlayPresetSound2() { cube?.PlayPresetSound(2); }
        public void LedOn()
        {
            List<Cube.LightOperation> scenario = new List<Cube.LightOperation>();
            float rad = (Mathf.Deg2Rad * (360.0f / 29.0f));
            for (int i = 0; i < 29; i++)
            {
                byte r = (byte)Mathf.Clamp((128 + (Mathf.Cos(rad * i) * 128)), 0, 255);
                byte g = (byte)Mathf.Clamp((128 + (Mathf.Sin(rad * i) * 128)), 0, 255);
                byte b = (byte)Mathf.Clamp(((Mathf.Abs(Mathf.Cos(rad * i) * 255))), 0, 255);
                scenario.Add(new Cube.LightOperation(100, r, g, b));
            }
            cube?.TurnOnLightWithScenario(0, scenario.ToArray());
        }
        public void LedOff() { cube?.TurnLedOff(); }
    }
}