// Assets/Scripts/WebGL/BridgeConnectorUI.cs
using UnityEngine;
using TMPro;

public class BridgeConnectorUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField ipInput;
    public TMP_Text statusText;

    [Header("Target")]
    public toio.Samples.Sample_Sensor.Sample_Sensor sampleSensor; 
    // ↑ 这里引用你场景里那个 Sample_Sensor 脚本对象（拖拽）

    private const string PREF_KEY = "BRIDGE_IP";

    void Start()
    {
        // 1) 读上次保存的IP
        var saved = PlayerPrefs.GetString(PREF_KEY, "127.0.0.1");
        ipInput.text = saved;
        statusText.text = $"IP: {saved} (not connected)";
    }

    // 给按钮绑定这个
    public void OnClickConnect()
    {
        var ip = ipInput.text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            statusText.text = "Please enter Bridge IP";
            return;
        }

        // 2) 保存
        PlayerPrefs.SetString(PREF_KEY, ip);
        PlayerPrefs.Save();

        // 3) 让 Sample_Sensor 去连
        statusText.text = $"Connecting to ws://{ip}:9001 ...";
        sampleSensor.ConnectToBridge(ip, onDone: (ok, msg) =>
        {
            statusText.text = ok ? $"Connected: {msg}" : $"Failed: {msg}";
        });
    }
}