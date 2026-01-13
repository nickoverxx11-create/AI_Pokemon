using TMPro;
using UnityEngine;

public class ESP32IPUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField ipInput;

    private void Start()
    {
        ipInput.text = ESP32Controller.Instance.GetESP32IP();
    }
    
    public void ApplyIP()
    {
        ESP32Controller.Instance.SetESP32IP(ipInput.text);
    }
}