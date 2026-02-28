#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using WebSocketSharp.Server;
using toio;
using toio.Samples.Sample_ConnectName;
using Cysharp.Threading.Tasks;

public class BridgeRunner : MonoBehaviour
{
    public int port = 9001;
    public ConnectType connectType = ConnectType.Real;

    private Cube cube;
    private WebSocketServer wss;
    private WebSocketServiceHost host;

    private float lastSend;

    // ✅ 用回调维护稳定状态
    private bool cardPresent = false;
    private uint lastStandardId = 0;

    async void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        var peripheral = await new CubeScanner(connectType).NearestScan();
        cube = await new CubeConnecter(connectType).Connect(peripheral);

        // ✅ 关键：用事件，不要用 standardId!=0
        cube.standardIdCallback.AddListener("Bridge_SID", c =>
        {
            cardPresent = true;
            lastStandardId = c.standardId;
        });
        cube.standardIdMissedCallback.AddListener("Bridge_SID_Missed", c =>
        {
            cardPresent = false;
            lastStandardId = 0;
        });

        await cube.ConfigMotorRead(true);
        await cube.ConfigIDNotification(500, Cube.IDNotificationType.OnChanged);
        await cube.ConfigIDMissedNotification(500);

        wss = new WebSocketServer(port);
        wss.AddWebSocketService("/", () => new BridgeWs(cube));
        wss.Start();

        host = wss.WebSocketServices["/"];
        Debug.Log($"[Bridge] ws://0.0.0.0:{port}/");
    }

    void Update()
    {
        if (cube == null || host == null) return;
        if (Time.realtimeSinceStartup - lastSend < 0.1f) return;
        lastSend = Time.realtimeSinceStartup;
        int degValue = cube.angle; // ✅ cube 没有 deg 就用 angle

        string msg =
            $"{{\"type\":\"state\",\"x\":{cube.x},\"y\":{cube.y},\"deg\":{degValue}," +
            $"\"standardId\":{lastStandardId},\"cardPresent\":{(cardPresent ? "true" : "false")}}}";

        host.Sessions.Broadcast(msg);
    }

    private void OnDestroy()
    {
        if (wss != null && wss.IsListening) wss.Stop();
    }
}
#endif