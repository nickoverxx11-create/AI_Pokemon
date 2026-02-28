// Scripts/CubeBackend/RealToioBackend.cs
#if !UNITY_WEBGL
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using toio.Samples.Sample_ConnectName; // 你项目里已有
using toio;

public class RealToioBackend : ICubeBackend
{
    public bool IsReady { get; private set; }
    public int X => cube?.x ?? 0;
    public int Y => cube?.y ?? 0;
    public int Deg => cube?.angle ?? 0;
    public uint StandardId => cube?.standardId ?? 0;
    public bool CardPresent { get; private set; }

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<uint> OnStandardIdDetected;
    public event Action OnStandardIdMissed;

    private Cube cube;
    private bool present;

    public async UniTask Connect(ConnectType connectType)
    {
        var peripheral = await new CubeScanner(connectType).NearestScan();
        cube = await new CubeConnecter(connectType).Connect(peripheral);

        // 监听卡片出现/消失
        cube.standardIdCallback.AddListener("Bridge_SID", (c) =>
        {
            if (!present)
            {
                present = true;
                CardPresent = true;
                OnStandardIdDetected?.Invoke(c.standardId);
            }
        });
        cube.standardIdMissedCallback.AddListener("Bridge_SID_Missed", (c) =>
        {
            present = false;
            CardPresent = false;
            OnStandardIdMissed?.Invoke();
        });

        IsReady = true;
        OnConnected?.Invoke();
    }

    public void Move(int left, int right, int durationMs) => cube?.Move(left, right, durationMs);
    public void TargetMove(int x, int y, int angle) => cube?.TargetMove(x, y, angle);

    public void Tick() { /* nothing */ }
}
#endif