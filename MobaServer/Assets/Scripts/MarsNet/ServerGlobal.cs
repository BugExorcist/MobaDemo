using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
public class ServerGlobal {


	private static ServerGlobal instance;
	private List<Action> list_action = new List<Action> ();
	private Mutex mutex_actionList = new Mutex ();

	public string serverIp;
	public static ServerGlobal Instance
	{
		get{ 
			if (instance == null)
			{
				instance = new ServerGlobal();
			}
			return instance;
		}
	}

	private ServerGlobal()
	{
		GameObject obj = new GameObject ("ServerGlobal");
		obj.AddComponent<ServerUpdate> ();
		GameObject.DontDestroyOnLoad (obj);

		serverIp = GetLocalIPAddress();

    }

	/// <summary>
	/// 获取本机IP
	/// </summary>
    public string GetLocalIPAddress()
    {
        // 获取本机的主机名
        var host = Dns.GetHostEntry(Dns.GetHostName());

        // 遍历与该主机关联的所有IP地址
        foreach (var ip in host.AddressList)
        {
            // 筛选出IPv4地址
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        // 如果没有找到IPv4地址，则抛出异常
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }

    public void Destory(){
		instance = null;
	}


	public void AddAction (Action _action)
	{
		mutex_actionList.WaitOne();
		list_action.Add (_action);
		mutex_actionList.ReleaseMutex ();
	}

	public void DoForAction ()
	{
		mutex_actionList.WaitOne ();
		for (int i = 0; i < list_action.Count; i++) {
			list_action [i] ();
		}
		list_action.Clear ();
		mutex_actionList.ReleaseMutex ();
	}

}


public class ServerUpdate : MonoBehaviour
{
	void Update ()
	{
		ServerGlobal.Instance.DoForAction ();
	}

	void OnApplicationQuit ()
	{
		LogManage.Instance.Destory ();
		ServerTcp.Instance.EndServer ();
		BattleManage.Instance.Destroy ();
	}
}
