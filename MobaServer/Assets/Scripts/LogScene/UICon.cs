using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICon : MonoBehaviour
{
    public TMP_Text serverIP;
    public TMP_InputField input;
    public Button startServer;

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        serverIP.text = ServerGlobal.Instance.serverIp;
        ServerConfig.battleUserNum = PlayerPrefs.GetInt("battleNumber", 1);// 默认值：1
        input.text = ServerConfig.battleUserNum.ToString();
        Debug.Log("Server start!");
    }

    public void StartServer()
    {
        int _number;
        if (int.TryParse(input.text, out _number))
        {
            ServerConfig.battleUserNum = _number;
            PlayerPrefs.SetInt("battleNumber", _number);
        }

        input.interactable = false;
        startServer.interactable = false;

        ServerTcp.Instance.StartServer();
    }
}
