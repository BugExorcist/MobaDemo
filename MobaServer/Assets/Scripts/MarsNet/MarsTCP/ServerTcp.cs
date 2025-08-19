using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ServerTcp
{
    static Socket serverSocket;
    private bool isRun = false;
    private Dictionary<string, Socket> dic_clientSocket = new Dictionary<string, Socket>();

    private static readonly object stLookObj = new object();
    private static ServerTcp instance;
    public static ServerTcp Instance
    { 
        get
        {
            lock (stLookObj)
            {
                if (instance == null)
                {
                    instance = new ServerTcp();
                }
            }
            return instance;
        }
    }

    private ServerTcp()
    {
        
    }

    public void Destory()
    {
        instance = null;
    }
    
    /// <summary>
    /// 启动Tcp服务
    /// </summary>
    public void StartServer()
    {
        try
        {
            IPAddress ip = IPAddress.Parse(ServerGlobal.Instance.serverIp);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(new IPEndPoint(ip, ServerConfig.servePort));// 绑定ip和端口
            serverSocket.Listen(20); // 最大连接数
            Debug.Log("启动监听" + serverSocket.LocalEndPoint.ToString() + "成功");
            isRun = true;

            Thread myThread = new Thread(ListenClientConnect);
            myThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("Tcp服务器启动失败：" + e.ToString());
        }
    }

    /// <summary>
    /// 监听客户端连接
    /// </summary>
    private void ListenClientConnect()
    {
        while(isRun)
        {
            try
            {
                Socket clientSocket = serverSocket.Accept();
                // Debug.Log("有客户端连接：" + clientSocket.RemoteEndPoint.ToString());
                Thread receiveThread = new Thread(ReceiveMessage);// 创建一个接收消息的线程
                receiveThread.Start(clientSocket);// 启动接收消息的线程
            }
            catch (Exception e)
            {
                Debug.Log("监听客户端连接异常：" + e.ToString());
            }
        }
        Debug.Log("监听客户端连接结束");
    }

    private void ReceiveMessage(object clientSocket)
    {
        Socket myClientSocket = (Socket)clientSocket;
        // Debug.Log(myClientSocket.RemoteEndPoint.ToString());
        string ipEndPoint = myClientSocket.RemoteEndPoint.ToString();// 获取客户端IP+端口
        string _socketIp = ipEndPoint.Split(':')[0];// 截取客户端IP
        Debug.Log("有客户端连接:" + _socketIp);
        dic_clientSocket[_socketIp] = myClientSocket;// 使用ip存储客户端socket

        bool _flag = true;// 客户端连接状态

        byte[] resultData = new byte[1024];// 接收数据缓存
        while (isRun && _flag)// 服务器没有停止且客户端没有断开连接
        {
            try
            {
                //Debug.Log("_socketName是否连接:" + myClientSocket.Connected);
                //通过clientSocket接收数据  

                // ****对于 SelectRead，如果已调用 Socket.Listen()
                // 且连接处于挂起状态、数据可供读取，或者连接已关闭、重置或终止，则返回 true
                // 也就是说 有消息会返回true  服务器断开也会返回true
                // 对于数据可供读取 是客服端发送消息频率太高，超出了服务器处理缓冲区的极限（缓冲区有数据堆积）
                // 又收到了客户端发送的消息 即 数据可供读取，但处理不过来，会返回true

                // 此处问题的详细描述:
                // https://blog.csdn.net/m0_72474501/article/details/150538032

                // 写法1：
                //if (myClientSocket.Poll(1000, SelectMode.SelectRead))// 检测socket是否有数据可读
                //{
                //    if (myClientSocket.Available == 0)
                //    {
                //        // Poll + Available == 0 → 真正关闭
                //        // Poll + Available > 0 → 有数据，继续处理
                //        // Poll == false → 没数据，循环等待

                //        throw new Exception("客户端关闭了1~");
                //    }
                //    // 否则说明只是有数据可读，继续走下面的 Receive
                //}

                // 写法2：
                if (!myClientSocket.Poll(-1, SelectMode.SelectRead))// -1 永远阻塞 直到读到数据
                {
                    throw new Exception("客户端关闭了1~");
                }



                int _size = myClientSocket.Receive(resultData);// 接收数据写入缓存区

                if (_size <= 0)// 客户端手动关闭了连接
                {
                    throw new Exception("客户端关闭了2~");
                }

                // 拆包
                byte packMessageId = resultData[PackageConstant.PackMessageIdOffset];//消息id (1个字节)
                Int16 packlength = BitConverter.ToInt16(resultData, PackageConstant.PacklengthOffset);//消息包长度 (2个字节)
                int bodyDataLenth = packlength - PackageConstant.PacketHeadLength; // 包体 = 消息包长度 - 消息头长度。
                byte[] bodyData = new byte[bodyDataLenth];
                Array.Copy(resultData, PackageConstant.PacketHeadLength, bodyData, 0, bodyDataLenth);

                // 处理消息
                TcpPB.Instance().AnalyzeMessage((PBCommon.CSID)packMessageId, bodyData, _socketIp);
            }
            catch (Exception ex)
            {
                Debug.Log(_socketIp + "接收客户端数据异常: " + ex.Message);

                _flag = false;
                break;
            }
        }

        CloseClientTcp(_socketIp);
    }

    /// <summary>
    /// 关闭客户端TCP
    /// </summary>
    private void CloseClientTcp(string socketIp)
    {
        try
        {
            if (dic_clientSocket.ContainsKey(socketIp))
            {
                if (dic_clientSocket[socketIp] != null)
                {
                    dic_clientSocket[socketIp].Close();
                }
                dic_clientSocket.Remove(socketIp);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("关闭客户端..." + ex.Message);
        }
    }

    /// <summary>
    /// 停止TCP服务器
    /// </summary>
    public void EndServer()
    {

        if (!isRun)
        {
            return;
        }

        isRun = false;
        try
        {
            foreach (var item in dic_clientSocket)
            {
                item.Value.Close();
            }

            dic_clientSocket.Clear();

            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket = null;
            }
            Debug.Log("Server stop!");
        }
        catch (Exception ex)
        {
            Debug.Log("tcp服务器关闭失败:" + ex.Message);
        }
    }

    /// <summary>
    /// 向指定客户端发送消息
    /// </summary>
    /// <param name="_socketName">客户端的IP</param>
    public void SendMessage(string _socketName, byte[] _mes)
    {
        Debug.Log("SendMessage aaa  ----- _socketName  " + _socketName);
        if (isRun)
        {
            try
            {
                dic_clientSocket[_socketName].Send(_mes);
            }
            catch (Exception ex)
            {
                Debug.Log("发数据给异常:" + ex.Message);
            }
        }
    }

    // ----工具----

    /// <summary>
    /// 获取建立连接的客户端数量
    /// </summary>
    public int GetClientCount()
    {
        return dic_clientSocket.Count;
    }

    /// <summary>
    /// 获取所有建立连接的客户端IP
    /// </summary>
    public List<string> GetAllClientIp()
    {
        return new List<string>(dic_clientSocket.Keys);
    }
}
