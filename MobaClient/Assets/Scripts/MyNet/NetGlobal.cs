using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
public class NetGlobal
{
    // 把所有的收到的消息放在队列中，每帧处理

    private static NetGlobal singleInstance;
    private List<Action> list_action = new List<Action>();
    private Mutex mutex_actionList = new Mutex();

    public string serverIP;
    public int udpSendPort;// 服务器的udp端口
    public int userUid;
    public static NetGlobal Instance()
    {
        // 如果类的实例不存在则创建，否则直接返回
        if (singleInstance == null)
        {
            singleInstance = new NetGlobal();
        }
        return singleInstance;
    }

    private NetGlobal()
    {
        GameObject obj = new GameObject("NetGlobal");
        obj.AddComponent<NetUpdate>();
        GameObject.DontDestroyOnLoad(obj);
    }

    public void Destory()
    {
        singleInstance = null;
    }

    /// <summary>
    /// 把消息添加到消息队列中
    /// </summary>
    public void AddAction(Action _action)
    {
        mutex_actionList.WaitOne();
        list_action.Add(_action);
        mutex_actionList.ReleaseMutex();
    }

    /// <summary>
    /// 执行消息队列中的消息
    /// </summary>
    public void DoForAction()
    {
        mutex_actionList.WaitOne();
        for (int i = 0; i < list_action.Count; i++)
        {
            list_action[i]();
        }
        list_action.Clear();
        mutex_actionList.ReleaseMutex();
    }

}


public class NetUpdate : MonoBehaviour
{
    void Update()
    {
        NetGlobal.Instance().DoForAction();
    }

    void OnApplicationQuit()
    {

        MyTcp.Instance.EndClient();
        UdpPB.Instance().Destory();
    }
}
