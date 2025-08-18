
using System.IO;
using System.Collections.Generic;
using System;

//包结构 [消息ID(1字节)] [包长度(2字节, short)] [包体(N字节)]
public struct PackageConstant
{
    // 消息id (1个字节 定义在PBCommon中)
    public static int PackMessageIdOffset = 0;
    //消息包长度 (2个字节 记录 包头长度(3 bytes)+包体长度(N bytes))
    public static int PacklengthOffset = 1;
    //包头长度
    public static int PacketHeadLength = 3;
}
	
public class CSData
{
    /// <summary>
    /// 打包发送数据，把数据打包成自定义的数据包格式（id + 长度 + 包体）
    /// </summary>
    /// <typeparam name="T">protobuf消息名称</typeparam>
    /// <param name="pb_Body">消息体</param>
    /// <param name="messageID">消息类型id</param>
    /// <returns></returns>
    public static byte[] GetSendMessage<T> (T pb_Body, PBCommon.SCID messageID)
	{
		byte[] packageBody = CSData.SerializeData<T> (pb_Body);
		byte packMessageId = (byte)messageID; //消息id (1个字节)

		int packlength = PackageConstant.PacketHeadLength + packageBody.Length; //消息包长度 (2个字节)
		byte[] packlengthByte = BitConverter.GetBytes((short)packlength);

		List<byte> packageHeadList = new List<byte>();
		//包头信息
		packageHeadList.Add(packMessageId);
		packageHeadList.AddRange(packlengthByte);
		//包体
		packageHeadList.AddRange(packageBody);

		return packageHeadList.ToArray ();
	}

    /// <summary>
    /// 把 Protobuf 消息对象序列化成 protobuf 二进制
    /// </summary>
    public static byte[] SerializeData<T> (T instance)
	{
		byte[] bytes;
		using (var ms = new MemoryStream ()) 
		{
			ProtoBuf.Serializer.Serialize (ms, instance);
			bytes = new byte[ms.Position];
			var fullBytes = ms.GetBuffer ();
			Array.Copy (fullBytes, bytes, bytes.Length);
		}
		return bytes;
	}

	/// <summary>
	/// 把 protobuf 二进制反序列化成 Protobuf 消息对象
	/// </summary>
	/// <typeparam name="T">Protobuf 消息对象</typeparam>
	/// <param name="bytes">protobuf 二进制</param>
	public static T DeserializeData<T> (byte[] bytes)
    {
        using (Stream ms = new MemoryStream(bytes))
        {
			return ProtoBuf.Serializer.Deserialize<T> (ms);
		}
	}
}
