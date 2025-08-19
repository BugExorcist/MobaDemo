using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PBCommon;
using PBLogin;
using TMPro;
public class LoginCon : MonoBehaviour
{
    public TMP_InputField inputField;
    public GameObject waitTip;

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 30; // 限定游戏帧率
        inputField.text = "192.168.147.1";// 服务器IP
        NetGlobal.Instance(); // 执行方法的类

        // 注册这个消息的处理回调，当接收到这个消息时，会调用这个方法
        TcpPB.Instance().mes_login_result = Message_Login_Result;
        waitTip.SetActive(false);
    }


    public void OnClickLogin()
    {

        waitTip.SetActive(true);

        string _ip = inputField.text;
        MyTcp.Instance.ConnectServer(_ip, (_result) =>
        {
            if (_result)
            {
                Debug.Log("连接成功");
                NetGlobal.Instance().serverIP = _ip;
                TcpLogin _loginInfo = new TcpLogin();// 生成一个Ptotobuf消息的实例
                _loginInfo.token = SystemInfo.deviceUniqueIdentifier; // 客户端凭证
                                                                      // 这里的token可以从多个维度来生成，增加安全性，或者接入平台的sdk

                // 连接成功后发送消息
                MyTcp.Instance.SendMessage(CSData.GetSendMessage<TcpLogin>(_loginInfo, CSID.TCP_LOGIN));
            }
            else
            {
                Debug.Log("连接失败");
                waitTip.SetActive(false);
            }
        });
    }


    void Message_Login_Result(TcpResponseLogin _mes)
    {
        if (_mes.result)
        {
            NetGlobal.Instance().userUid = _mes.uid;
            NetGlobal.Instance().udpSendPort = _mes.udpPort; // tcp登录成功，记录udp端口
            Debug.Log("登录成功～～～" + NetGlobal.Instance().userUid);
            ClearSceneData.LoadScene(GameConfig.mainScene);
        }
        else
        {
            Debug.Log("登录失败～～～");
            waitTip.SetActive(false);
        }
    }

    void OnDestroy()
    {
        TcpPB.Instance().mes_login_result = null;
    }
}
