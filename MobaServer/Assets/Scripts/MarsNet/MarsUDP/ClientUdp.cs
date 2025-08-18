using UnityEngine;
using System;
using System.Text;
using System.Threading;

using System.Net;
using System.Net.Sockets;

public class ClientUdp {
	public delegate void DelegateAnalyzeMessage(PBCommon.CSID messageId,byte[] bodyData);
	public DelegateAnalyzeMessage delegate_analyze_message;

	public int userUid;
	private int sendPortNum;
	private UdpClient sendClient = null;
	private IPEndPoint sendEndPort;
	private bool isRun;
	private string serverIp;

	public void StartClientUdp(string _ip,int _uid){

		if (sendEndPort != null) {
			Debug.Log ("客户端udp已经启动~");
			return;
		}

		userUid = _uid;
		serverIp = _ip;
		isRun = true;

		sendClient = UdpManager.Instance.GetClient();
		Debug.Log("sendClient  " + sendClient.Client.Connected);
//		sendClient = new UdpClient(NormalData.recvPort);
//		sendEndPort = new IPEndPoint(IPAddress.Parse(_ip), ServerConfig.udpRecvPort);	

		Thread t = new Thread(new ThreadStart(RecvThread));
		t.Start();


	}

	public void EndClientUdp(){
		try {
			isRun = false;
			if (sendEndPort != null) {
				UdpManager.Instance.CloseUdpClient();
				sendClient = null;
				sendEndPort = null;
			}
			delegate_analyze_message = null;	
		} catch (Exception ex) {
			Debug.Log ("udp连接关闭异常:" + ex.Message);
		}

	}

	private void CreatSendEndPort(int _port){
		sendEndPort = new IPEndPoint(IPAddress.Parse(serverIp), _port);
	}

	public void SendMessage(byte[] _mes){
		if (isRun) {
			try {
				sendClient.Send (_mes,_mes.Length,sendEndPort);	
//				GameData.Instance().sendNum+=_mes.Length;
				//				Debug.Log("发送量:" + _mes.Length.ToString() + "," + GameData.Instance().sendNum.ToString());
			} catch (Exception ex) {
				Debug.Log ("udp发送失败:" + ex.Message);
			}

		}
	}


	public void RecvClientReady(int _userUid){
		if (_userUid == userUid && sendEndPort == null) {
			CreatSendEndPort(sendPortNum);
		}
	}
    // 接收线程不断 监听客户端的消息
	private void RecvThread()
	{

		IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(serverIp), UdpManager.Instance.recvPort);
		while (isRun)
		{
			int i = 0;
			try {
				i++;
			
				byte[] buf = sendClient.Receive(ref endpoint);
				i++;
				if (sendEndPort == null) {
					Debug.Log("接收客户端udp信息:" + endpoint.Port);
					sendPortNum = endpoint.Port;
				}
				Debug.Log("R 11 " + buf);
				i++;
				byte packMessageId = buf[PackageConstant.PackMessageIdOffset];     //消息id (1个字节)

				Debug.Log("R 12 " + buf.Length);
				i++;
				Int16 packlength = BitConverter.ToInt16(buf,PackageConstant.PacklengthOffset);  //消息包长度 (2个字节)

				Debug.Log("R 13 "  + packlength);
				i++;
				int bodyDataLenth = packlength - PackageConstant.PacketHeadLength;
				byte[] bodyData = new byte[bodyDataLenth];

				Debug.Log("R 14" + packlength);
				i++;
				Array.Copy(buf, PackageConstant.PacketHeadLength, bodyData, 0, bodyDataLenth);

				Debug.Log("R 15" + packlength);
				i++;
				delegate_analyze_message((PBCommon.CSID)packMessageId,bodyData);
				Debug.Log("R 16" + packlength);
				i++;
				//是客户端,统计接收量
				//				GameData.Instance().recvNum+=buf.Length;
				//				Debug.Log("发送量:" + buf.Length.ToString() + "," + GameData.Instance().recvNum.ToString());
			} catch (Exception ex) {
				Debug.Log("sendClient "  + sendClient);
				Debug.Log("sendClient " + sendClient.Client.Connected); 
				isRun = false;
				Debug.Log( i +" ==i  "+endpoint.Address + "udpRecvThread 失败:" + ex.Message);
			}
		}
		Debug.Log ("udp接收线程退出~~~~~");
	}


	void OnDestroy()
	{
		EndClientUdp ();
	}
}