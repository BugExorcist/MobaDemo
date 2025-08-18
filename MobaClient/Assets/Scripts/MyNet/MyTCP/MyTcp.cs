using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public class MyTcp
{
    private static MyTcp singleInstance;
    private static readonly object padlock = new object();

    private byte[] result = new byte[1024];
    private Socket clientSocket;

    public bool isRun = false;

    private Action<bool> ac_connect;
    public static MyTcp Instance
    {
        get
        {
            lock (padlock)  // 加锁保证单例唯一
            {
                if (singleInstance == null)
                {
                    singleInstance = new MyTcp();
                }
                return singleInstance;
            }
        }
    }

    private MyTcp()
    {

    }

    public void Destory()
    {

        singleInstance = null;
    }

    public void EndClient()
    {

        isRun = false;

        if (clientSocket != null)
        {
            try
            {
                clientSocket.Close();
                clientSocket = null;
                Debug.Log("关闭tcp连接");
            }
            catch (Exception ex)
            {
                Debug.Log("关闭tcp连接异常111:" + ex.Message);
            }
        }
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="_ip">服务器ID</param>
    /// <param name="_result">连接完成回调</param>
    public void ConnectServer(string _ip, Action<bool> _result)
    {
        //设定服务器IP地址
        ac_connect = _result;
        IPAddress ip;
        bool _isRight = IPAddress.TryParse(_ip, out ip);

        if (!_isRight)
        {
            Debug.Log("无效地址......" + _ip);
            _result(false);
            return;
        }
        // 创建客户端Socket对象 进行服务器连接
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint _endpoint = new IPEndPoint(ip, NetConfig.TcpServerPort);
        Debug.Log("开始连接tcp~");
        clientSocket.BeginConnect(_endpoint, requestConnectCallBack, clientSocket);
    }

    /// <summary>
    /// 连接服务器的回调
    /// </summary>
    private void requestConnectCallBack(IAsyncResult iar)
    {
        try
        {
            //还原原始的TcpClient对象
            Socket client = (Socket)iar.AsyncState;
            // 完成连接
            client.EndConnect(iar);

            Debug.Log("连接服务器成功:" + client.RemoteEndPoint.ToString());
            isRun = true;

            NetGlobal.Instance().AddAction(() =>
            {
                if (ac_connect != null)
                {
                    ac_connect(true);// 调用连接服务器的回调方法，告诉上层连接成功
                }
            });

            Thread myThread = new Thread(ReceiveMessage);
            myThread.Start();
        }
        catch (Exception e)
        {
            NetGlobal.Instance().AddAction(() =>
            {
                if (ac_connect != null)
                {
                    ac_connect(false);
                }
            });

            Debug.Log("tcp连接异常:" + e.Message);
        }
        finally
        {

        }
    }

    /// <summary>
    /// 接收消息 原理同服务器
    /// </summary>
    private void ReceiveMessage()
    {

        while (isRun)
        {
            try
            {

                if (!clientSocket.Connected)
                {
                    throw new Exception("tcp客户端关闭了~~~");
                }

                //通过clientSocket接收数据 
                int _size = clientSocket.Receive(result);

                if (_size <= 0)
                {
                    throw new Exception("客户端关闭了2~");
                }


                byte packMessageId = result[PackageConstant.PackMessageIdOffset];     //消息id (1个字节)
                Int16 packlength = BitConverter.ToInt16(result, PackageConstant.PacklengthOffset);  //消息包长度 (2个字节)
                int bodyDataLenth = packlength - PackageConstant.PacketHeadLength;  // 计算包体长度
                byte[] bodyData = new byte[bodyDataLenth];
                Array.Copy(result, PackageConstant.PacketHeadLength, bodyData, 0, bodyDataLenth);

                TcpPB.Instance().AnalyzeMessage((PBCommon.SCID)packMessageId, bodyData);
            }
            catch (Exception ex)
            {
                Debug.Log("接收服务端数据异常:" + ex.Message);
                EndClient();
                break;
            }
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public void SendMessage(byte[] _mes)
    {
        if (isRun)
        {
            try
            {
                clientSocket.Send(_mes);
            }
            catch (Exception ex)
            {
                EndClient();
                Debug.Log("发送数据异常:" + ex.Message);
            }
        }
    }

}
