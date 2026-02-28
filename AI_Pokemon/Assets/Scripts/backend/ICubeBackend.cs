// Scripts/backend/ICubeBackend.cs
using System;

public interface ICubeBackend
{
    bool IsReady { get; }
    int X { get; }
    int Y { get; }
    int Deg { get; }
    uint StandardId { get; }
    bool CardPresent { get; }

    event Action OnConnected;
    event Action OnDisconnected;
    event Action<uint> OnStandardIdDetected;
    event Action OnStandardIdMissed;
    void Move(int left, int right, int durationMs);
    void TargetMove(int x, int y, int angle);

    void Tick(); // WebGL: dispatch message queue
}