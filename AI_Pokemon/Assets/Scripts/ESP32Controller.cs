using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ESP32Controller : MonoBehaviour
{
    public static ESP32Controller Instance;

    [Header("ESP32 Settings")]
    [SerializeField] private string esp32IP = "http://172.20.10.5";

    private const string PrefKey = "ESP32_IP";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        if (PlayerPrefs.HasKey(PrefKey))
            esp32IP = PlayerPrefs.GetString(PrefKey);
    }

    public string GetESP32IP() => esp32IP;

    public void SetESP32IP(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return;

        ip = ip.Trim();

        if (!ip.StartsWith("http://") && !ip.StartsWith("https://"))
            ip = "http://" + ip;

        esp32IP = ip;
        
        PlayerPrefs.SetString(PrefKey, esp32IP);
        PlayerPrefs.Save();

        Debug.Log("ESP32 IP set to: " + esp32IP);
    }

    public void SendLEDData(string csvData)
    {
        StartCoroutine(PostLEDData(csvData));
    }

    IEnumerator PostLEDData(string csv)
    {
        using (UnityWebRequest www = new UnityWebRequest(esp32IP + "/leds", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(csv);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "text/plain");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("LED data sent successfully!");
            else
                Debug.LogError("Error sending LED data: " + www.error);
        }
    }
}


