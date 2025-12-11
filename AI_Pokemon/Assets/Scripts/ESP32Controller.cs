using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ESP32Controller : MonoBehaviour
{
    public static ESP32Controller Instance;

    public string esp32IP = "http://172.20.10.3";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
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
            {
                Debug.Log("LED data sent successfully!");
            }
            else
            {
                Debug.LogError("Error sending LED data: " + www.error);
            }
        }
    }
}






