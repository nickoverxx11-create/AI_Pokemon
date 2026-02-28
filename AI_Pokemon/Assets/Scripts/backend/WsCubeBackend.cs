#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using UnityEngine;
using NativeWebSocket;

[Serializable] public class MsgBase { public string type; }
[Serializable] public class StateMsg : MsgBase
{
    public int x, y, deg;
    public uint standardId;
    public bool cardPresent;
}

public class WsCubeBackend : ICubeBackend
{
    private WebSocket ws;

    public bool IsReady { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Deg { get; private set; }
    public uint StandardId { get; private set; }
    public bool CardPresent { get; private set; }

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<uint> OnStandardIdDetected;
    public event Action OnStandardIdMissed;

    // ✅ 关键：必须是字段（跨消息保存）
    private bool _prevPresent = false;
    private uint _prevId = 0;

    public WsCubeBackend(string wsUrl)
    {
        ws = new WebSocket(wsUrl);

        ws.OnOpen += () =>
        {
            Debug.Log("[WS] OPEN");
            IsReady = true;
            OnConnected?.Invoke();
        };

        ws.OnClose += (e) =>
        {
            Debug.Log("[WS] CLOSE");
            IsReady = false;
            OnDisconnected?.Invoke();
        };

        // 如果你的 NativeWebSocket 有 OnError，可以加：
        try
        {
            ws.OnError += (err) => Debug.LogError("[WS] ERROR: " + err);
        }
        catch { }

        ws.OnMessage += (bytes) =>
        {
            var json = System.Text.Encoding.UTF8.GetString(bytes);

            MsgBase b;
            try { b = JsonUtility.FromJson<MsgBase>(json); }
            catch { return; }
            if (b == null || b.type != "state") return;

            StateMsg s;
            try { s = JsonUtility.FromJson<StateMsg>(json); }
            catch { return; }
            if (s == null) return;

            X = s.x; Y = s.y; Deg = s.deg;
            CardPresent = s.cardPresent;
            StandardId = s.standardId;

            // ✅ 边沿触发：0->1、1->0、换卡
            if (!_prevPresent && CardPresent)
                OnStandardIdDetected?.Invoke(StandardId);

            if (_prevPresent && !CardPresent)
                OnStandardIdMissed?.Invoke();

            if (_prevPresent && CardPresent && StandardId != _prevId)
                OnStandardIdDetected?.Invoke(StandardId);

            _prevPresent = CardPresent;
            _prevId = StandardId;
        };

        ws.Connect();
    }

    public void Move(int left, int right, int durationMs)
    {
        if (!IsReady) return;
        Debug.Log($"[WS] send move l={left} r={right} ms={durationMs}");
        ws.SendText($"{{\"type\":\"move\",\"l\":{left},\"r\":{right},\"ms\":{durationMs}}}");
    }

    public void TargetMove(int x, int y, int angle)
    {
        if (!IsReady) return;
        Debug.Log($"[WS] send targetMove x={x} y={y} a={angle}");
        ws.SendText($"{{\"type\":\"targetMove\",\"x\":{x},\"y\":{y},\"angle\":{angle}}}");
    }

    public void Tick()
    {
        // 有些版本需要 dispatch
        try
        {
            var m = ws.GetType().GetMethod("DispatchMessageQueue");
            if (m != null) m.Invoke(ws, null);
        }
        catch { }
    }
}
#endif