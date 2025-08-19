using System.Collections;
using System.Collections.Generic;
using System.Threading;
using PBBattle;
using PBCommon;
using System;
using UnityEngine;
public class BattleCon
{
    #region 战斗基础属性
    /// <summary>
    /// 战斗唯一ID
    /// </summary>
    private int battleID;

    /// <summary>
    /// 玩家UID与战斗ID的映射字典
    /// key: 玩家UID
    /// value: 战斗中的玩家ID
    /// </summary>
    private Dictionary<int, int> dic_battleUserUid;

    /// <summary>
    /// 玩家战斗ID与UDP连接的映射字典
    /// key: 战斗中的玩家ID
    /// value: 对应玩家的UDP连接对象
    /// </summary>
    private Dictionary<int, ClientUdp> dic_udp;

    /// <summary>
    /// 玩家战斗ID与准备状态的映射字典
    /// key: 战斗中的玩家ID
    /// value: 是否准备就绪
    /// </summary>
    private Dictionary<int, bool> dic_battleReady;

    /// <summary>
    /// 战斗是否运行中
    /// </summary>
    private bool _isRun = false;

    /// <summary>
    /// 战斗是否已开始
    /// </summary>
    private bool isBeginBattle = false;

    /// <summary>
    /// 当前帧编号
    /// </summary>
    private int frameNum;

    /// <summary>
    /// 上一帧编号
    /// </summary>
    private int lastFrame;
    #endregion

    #region 帧同步相关属性
    /// <summary>
    /// 记录当前帧的玩家操作数组
    /// 索引为战斗中的玩家ID-1
    /// </summary>
    private PlayerOperation[] frameOperation;

    /// <summary>
    /// 记录玩家的消息ID数组
    /// 用于处理网络延迟和消息顺序问题
    /// </summary>
    private int[] playerMesNum;

    /// <summary>
    /// 记录玩家游戏结束状态的数组
    /// </summary>
    private bool[] playerGameOver;

    /// <summary>
    /// 是否有一个玩家已结束游戏
    /// </summary>
    private bool oneGameOver;

    /// <summary>
    /// 是否所有玩家都已结束游戏
    /// </summary>
    private bool allGameOver;

    /// <summary>
    /// 存储所有帧操作数据的字典
    /// key: 帧编号
    /// value: 该帧的所有玩家操作
    /// </summary>
    private Dictionary<int, AllPlayerOperation> dic_gameOperation = new Dictionary<int, AllPlayerOperation>();
    #endregion

    #region 战斗结束相关属性
    /// <summary>
    /// 等待战斗结束的计时器
    /// </summary>
    private Timer waitBattleFinish;

    /// <summary>
    /// 战斗结束倒计时时间(毫秒)
    /// </summary>
    private float finishTime;
    #endregion

    #region 战斗管理方法
    /// <summary>
    /// 创建战斗
    /// </summary>
    /// <param name="_battleID">战斗唯一ID</param>
    /// <param name="_battleUser">参与战斗的玩家列表</param>
    public void CreatBattle(int _battleID, List<MatchUserInfo> _battleUser)
    {
        // 帧同步要保证每个客户端的计算结果一致，服务器产生随机数种子，发送给客户端使用
        int randSeed = UnityEngine.Random.Range(0, 100);

        // 使用线程池处理战斗创建，避免阻塞主线程
        ThreadPool.QueueUserWorkItem((obj) =>
        {
            battleID = _battleID;
            dic_battleUserUid = new Dictionary<int, int>();// 玩家uid
            dic_udp = new Dictionary<int, ClientUdp>();// 玩家udp连接
            dic_battleReady = new Dictionary<int, bool>();// 玩家是否准备

            int userBattleID = 0;

            TcpEnterBattle _mes = new TcpEnterBattle();// 创建匹配 protobuf信息
            _mes.randSeed = randSeed;
            for (int i = 0; i < _battleUser.Count; i++)
            {
                int _userUid = _battleUser[i].uid;
                userBattleID++;  // 为每个user设置一个battleID，从1开始

                dic_battleUserUid[_userUid] = userBattleID;

                string _ip = UserManage.Instance.GetUserInfo(_userUid).socketIp;// 获取用户IP
                //var _udp = new ClientUdp ();
                //_udp.StartClientUdp (_ip,_userUid);    
                //_udp.delegate_analyze_message = AnalyzeMessage;
                //dic_udp [userBattleID] = _udp;
                //dic_battleReady[userBattleID] = false;

                BattleUserInfo _bUser = new BattleUserInfo();
                _bUser.uid = _userUid;
                _bUser.battleID = userBattleID;
                _bUser.roleID = _battleUser[i].roleID;

                _mes.battleUserInfo.Add(_bUser);
            }

            // 把战斗信息广播给所有玩家
            for (int i = 0; i < _battleUser.Count; i++)
            {
                int _userUid = _battleUser[i].uid;
                string _ip = UserManage.Instance.GetUserInfo(_userUid).socketIp;
                Debug.Log("TCP_ENTER_BATTLE  " + DateTime.Now.TimeOfDay.ToString());
                ServerTcp.Instance.SendMessage(_ip, CSData.GetSendMessage<TcpEnterBattle>(_mes, SCID.TCP_ENTER_BATTLE));
            }
        }, null);
    }

    /// <summary>
    /// 销毁战斗
    /// 清理所有UDP连接和战斗状态
    /// </summary>
    public void DestroyBattle()
    {
        foreach (var item in dic_udp.Values)
        {
            item.EndClientUdp();
        }

        _isRun = false;
    }

    /// <summary>
    /// 结束战斗
    /// 清理资源并通知战斗管理器
    /// </summary>
    private void FinishBattle()
    {
        foreach (var item in dic_udp.Values)
        {
            item.EndClientUdp();
        }

        BattleManage.Instance.FinishBattle(battleID);
    }

    /// <summary>
    /// 检查战斗是否可以开始
    /// 当所有玩家都准备就绪时开始战斗
    /// </summary>
    /// <param name="_userBattleID">准备就绪的玩家战斗ID</param>
    private void CheckBattleBegin(int _userBattleID)
    {
        if (isBeginBattle)
        {
            return;
        }

        dic_battleReady[_userBattleID] = true;

        isBeginBattle = true;
        // 检查所有玩家是否都已准备就绪
        foreach (var item in dic_battleReady.Values)
        {
            isBeginBattle = (isBeginBattle && item);
        }

        if (isBeginBattle)
        {
            Debug.Log("BeginBattle  **!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! ");
            //开始战斗
            BeginBattle();
        }
    }

    /// <summary>
    /// 开始战斗
    /// 初始化战斗状态并启动帧同步线程（每一个对局都有一个线程）
    /// </summary>
    void BeginBattle()
    {
        frameNum = 0;
        lastFrame = 0;
        _isRun = true;
        oneGameOver = false;
        allGameOver = false;

        int playerNum = dic_battleUserUid.Keys.Count;

        frameOperation = new PlayerOperation[playerNum];
        playerMesNum = new int[playerNum];
        playerGameOver = new bool[playerNum];
        for (int i = 0; i < playerNum; i++)
        {
            frameOperation[i] = null;
            playerMesNum[i] = 0;
            playerGameOver[i] = false;
        }

        // 启动发送帧数据的线程
        Thread _threadSenfd = new Thread(Thread_SendFrameData);
        _threadSenfd.Start();
    }
    #endregion

    #region 帧同步相关方法
    /// <summary>
    /// 发送帧数据的线程函数
    /// 负责向所有玩家广播帧操作数据
    /// </summary>
    private void Thread_SendFrameData()
    {
        //向玩家发送战斗开始
        bool isFinishBS = false;
        Debug.Log("Thread_SendFrameData dic_udp.Count****  " + dic_udp.Count);
        while (!isFinishBS)
        {
            // 向客户端广播战斗开始
            UdpBattleStart _btData = new UdpBattleStart();
            byte[] _data = CSData.GetSendMessage<UdpBattleStart>(_btData, SCID.UDP_BATTLE_START);
            foreach (var item in dic_udp)
            {
                Debug.Log("向玩家发送战斗 " + item.Value.userUid);
                item.Value.SendMessage(_data);
            }

            bool _allData = true;
            for (int i = 0; i < frameOperation.Length; i++)
            {
                if (frameOperation[i] == null)
                {
                    Debug.Log("Thread_SendFrameData 没有收到全部玩家的第一帧数据 ****  i= " + i);
                    _allData = false;// 有一个玩家没有发送上来操作 则判断为false
                    break;
                }
            }

            // 每一个玩家都上传了一帧数据
            if (_allData)
            {
                Debug.Log("战斗服务器:收到全部玩家的第一次操作数据 ");
                frameNum = 1;

                isFinishBS = true;
            }
            else
            {
                Debug.Log("NO 收到全部玩家的第一次操作数据 ");
            }

            Thread.Sleep(500);
        }

        Debug.Log("开始发送帧数据 ");
        // 正式游戏循环
        while (_isRun)
        {   // 收集每个玩家操作数据
            UdpDownFrameOperations _dataPb = new UdpDownFrameOperations();
            if (oneGameOver)
            {
                _dataPb.frameID = lastFrame;
                _dataPb.operations = dic_gameOperation[lastFrame];
            }
            else
            {
                _dataPb.operations = new AllPlayerOperation();
                _dataPb.operations.operations.AddRange(frameOperation);
                _dataPb.frameID = frameNum;
                dic_gameOperation[frameNum] = _dataPb.operations;
                lastFrame = frameNum;
                frameNum++;
            }

            byte[] _data = CSData.GetSendMessage<UdpDownFrameOperations>(_dataPb, SCID.UDP_DOWN_FRAME_OPERATIONS);
            foreach (var item in dic_udp)
            {
                int _index = item.Key - 1;
                if (!playerGameOver[_index])
                {
                    item.Value.SendMessage(_data);
                }
            }

            // 按照配置的帧时间休眠
            Thread.Sleep(ServerConfig.frameTime);
        }

        Debug.Log("帧数据发送线程结束.....................");
    }

    /// <summary>
    /// 更新玩家操作
    /// 处理玩家发送的操作数据，确保使用最新的操作
    /// </summary>
    /// <param name="_operation">玩家操作数据</param>
    /// <param name="_mesNum">消息ID，用于判断消息的新旧</param>
    public void UpdatePlayerOperation(PlayerOperation _operation, int _mesNum)
    {
        int _index = _operation.battleID - 1;
        // 只处理比当前存储的消息ID更新的消息
        if (_mesNum > playerMesNum[_index])
        {
            frameOperation[_index] = _operation;
            playerMesNum[_index] = _mesNum;
        }
        else
        {
            //早期的包就不记录了
        }
    }
    #endregion

    #region 战斗结束相关方法
    /// <summary>
    /// 更新玩家游戏结束状态
    /// 当所有玩家都结束游戏时，启动战斗结束倒计时
    /// </summary>
    /// <param name="_battleId">结束游戏的玩家战斗ID</param>
    public void UpdatePlayerGameOver(int _battleId)
    {
        oneGameOver = true;

        int _index = _battleId - 1;
        playerGameOver[_index] = true;

        allGameOver = true;
        for (int i = 0; i < playerGameOver.Length; i++)
        {
            if (playerGameOver[i] == false)
            {
                allGameOver = false;
                break;
            }
        }

        if (allGameOver)
        {
            _isRun = false;

            finishTime = 2000f;
            if (waitBattleFinish == null)
            {
                waitBattleFinish = new Timer(new TimerCallback(WaitClientFinish), null, 1000, 1000);
            }
        }
    }

    /// <summary>
    /// 等待客户端结束的回调函数
    /// 倒计时结束后正式结束战斗
    /// </summary>
    /// <param name="snder">计时器参数</param>
    void WaitClientFinish(object snder)
    {
        //		Debug.Log ("等待客户端结束～");
        finishTime -= 1000f;
        if (finishTime <= 0)
        {
            waitBattleFinish.Dispose();
            FinishBattle();
            //			Debug.Log ("战斗结束咯......");
        }
    }
    #endregion

    /// <summary>
    /// 接收战斗UDP消息
    /// 处理从客户端发送的各种战斗相关UDP消息
    /// </summary>
    /// <param name="messageId">消息ID，标识消息类型</param>
    /// <param name="bodyData">消息体数据</param>
    public void AnalyzeMessage(CSID messageId, byte[] bodyData)
    {
        Debug.Log("AnalyzeMessage   messageId == " + messageId);
        switch (messageId)
        {
            case CSID.UDP_BATTLE_READY:
                {
                    //接收战斗准备消息
                    UdpBattleReady _mes = CSData.DeserializeData<UdpBattleReady>(bodyData);
                    Debug.Log("pb_ReceiveMes.mesID == " + _mes.uid + "  接收战斗准备 ");
                    CheckBattleBegin(_mes.battleID);
                    Debug.Log("pb_ReceiveMes.mesID2 *** ");
                    dic_udp[_mes.battleID].RecvClientReady(_mes.uid);
                }
                break;
            case CSID.UDP_UP_PLAYER_OPERATIONS:
                {
                    //接收玩家操作消息
                    UdpUpPlayerOperations pb_ReceiveMes = CSData.DeserializeData<UdpUpPlayerOperations>(bodyData);
                    UpdatePlayerOperation(pb_ReceiveMes.operation, pb_ReceiveMes.mesID);
                }
                break;
            case CSID.UDP_UP_DELTA_FRAMES:
                {
                    //接收客户端请求的帧数据范围
                    UdpUpDeltaFrames pb_ReceiveMes = CSData.DeserializeData<UdpUpDeltaFrames>(bodyData);

                    UdpDownDeltaFrames _downData = new UdpDownDeltaFrames();

                    for (int i = 0; i < pb_ReceiveMes.frames.Count; i++)
                    {
                        int framIndex = pb_ReceiveMes.frames[i];

                        UdpDownFrameOperations _downOp = new UdpDownFrameOperations();
                        _downOp.frameID = framIndex;
                        _downOp.operations = dic_gameOperation[framIndex];

                        _downData.framesData.Add(_downOp);
                    }

                    byte[] _data = CSData.GetSendMessage<UdpDownDeltaFrames>(_downData, SCID.UDP_DOWN_DELTA_FRAMES);
                    dic_udp[pb_ReceiveMes.battleID].SendMessage(_data);
                }
                break;
            case CSID.UDP_UP_GAME_OVER:
                {
                    //接收玩家游戏结束消息
                    UdpUpGameOver pb_ReceiveMes = CSData.DeserializeData<UdpUpGameOver>(bodyData);
                    UpdatePlayerGameOver(pb_ReceiveMes.battleID);

                    UdpDownGameOver _downData = new UdpDownGameOver();
                    byte[] _data = CSData.GetSendMessage<UdpDownGameOver>(_downData, SCID.UDP_DOWN_GAME_OVER);
                    dic_udp[pb_ReceiveMes.battleID].SendMessage(_data);
                }
                break;
            default:
                break;
        }
    }
}
