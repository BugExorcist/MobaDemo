using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PBCommon;
using PBLogin;
using PBMatch;
public class TcpPB {
	private static TcpPB singleInstance;
	/// <summary>
	/// 获取TcpProtobuf实例
	/// </summary>
	public static TcpPB Instance()
	{
		// 如果类的实例不存在则创建，否则直接返回
		if (singleInstance == null)
		{
			singleInstance = new TcpPB();
		}
		return singleInstance;
	}

	private TcpPB()
	{

	}

	public void Destory(){
		singleInstance = null;
	}

	/// <summary>
	/// 解析收到的消息
	/// </summary>
	/// <param name="messageId">消息类型</param>
	/// <param name="bodyData">消息体</param>
	/// <param name="_socketIp">来源客户端IP</param>
	public void AnalyzeMessage(PBCommon.CSID messageId,byte[] bodyData,string _socketIp){
        Debug.Log("messageId   "  + messageId);
		switch (messageId)
		{
			case CSID.TCP_LOGIN:// 登录
				{
					TcpLogin _info = CSData.DeserializeData<TcpLogin>(bodyData);

					int _uid = UserManage.Instance.UserLogin(_info.token, _socketIp);// token登录(客户端唯一身份证明)
					TcpResponseLogin _result = new TcpResponseLogin();
					_result.result = true;
					_result.uid = _uid;
					_result.udpPort = UdpManager.Instance.recvPort;

					ServerTcp.Instance.SendMessage(_socketIp, CSData.GetSendMessage<TcpResponseLogin>(_result, SCID.TCP_RESPONSE_LOGIN));
				}
				break;
			case CSID.TCP_REQUEST_MATCH:// 请求匹配
				{
					TcpRequestMatch _mes = CSData.DeserializeData<TcpRequestMatch>(bodyData);
					MatchManage.Instance.NewMatchUser(_mes.uid, _mes.roleID);// 添加用户进入匹配队伍

					TcpResponseRequestMatch rmRes = new TcpResponseRequestMatch();
					ServerTcp.Instance.SendMessage(_socketIp, CSData.GetSendMessage<TcpResponseRequestMatch>(rmRes, SCID.TCP_RESPONSE_REQUEST_MATCH));
				}
				break;
			case CSID.TCP_CANCEL_MATCH:// 取消匹配
				{
					TcpCancelMatch _mes = CSData.DeserializeData<TcpCancelMatch>(bodyData);
					MatchManage.Instance.CancleMatch(_mes.uid);

					TcpResponseCancelMatch cmRes = new TcpResponseCancelMatch();
					ServerTcp.Instance.SendMessage(_socketIp, CSData.GetSendMessage<TcpResponseCancelMatch>(cmRes, SCID.TCP_RESPONSE_CANCEL_MATCH));
				}
				break;
			default:
				break;
		}
	}
}
