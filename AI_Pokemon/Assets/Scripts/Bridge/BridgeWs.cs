// Assets/Scripts/Bridge/BridgeWs.cs
#if UNITY_EDITOR || UNITY_STANDALONE
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using toio;
using UnityEngine;

public class BridgeWs : WebSocketBehavior
{
    private readonly Cube cube;

    public BridgeWs(Cube cube) { this.cube = cube; }

    protected override void OnOpen()
    {
        Send("{\"type\":\"connected\"}");
    }

    protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
    {
        var json = e.Data;
        Debug.Log("[Bridge] recv: " + json);
        if (json.Contains("\"type\":\"move\""))
        {
            int l = ExtractInt(json, "\"l\":");
            int r = ExtractInt(json, "\"r\":");
            int ms = ExtractInt(json, "\"ms\":");
            cube?.Move(l, r, ms);
        }
        else if (json.Contains("\"type\":\"targetMove\""))
        {
            int x = ExtractInt(json, "\"x\":");
            int y = ExtractInt(json, "\"y\":");
            int a = ExtractInt(json, "\"angle\":");
            cube?.TargetMove(x, y, a);
        }
    }

    private int ExtractInt(string json, string key)
    {
        int i = json.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return 0;
        i += key.Length;
        int j = i;
        while (j < json.Length && (char.IsDigit(json[j]) || json[j] == '-')) j++;
        int.TryParse(json.Substring(i, j - i), out int v);
        return v;
    }
}
#endif