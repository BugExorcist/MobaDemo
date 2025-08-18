using System.Collections;
using System.Collections.Generic;
using System.Threading;
using PBBattle;
using PBCommon;
using System;
using UnityEngine;
public class BattleCon
{
    private int battleID;
    private Dictionary<int, int> dic_battleUserUid;
    private Dictionary<int, ClientUdp> dic_udp;

    private Dictionary<int, bool> dic_battleReady;

    private bool _isRun = false;
    private bool isBeginBattle = false;
    private int frameNum;
    private int lastFrame;

    private PlayerOperation[] frameOperation;//记录当前帧的玩家操作
    private int[] playerMesNum;//记录玩家的包id
    private bool[] playerGameOver;//记录玩家游戏结束
    private bool oneGameOver;
    private bool allGameOver;

    private Dictionary<int, AllPlayerOperation> dic_gameOperation = new Dictionary<int, AllPlayerOperation>();


    private Timer waitBattleFinish;
    private float finishTime;//结束倒计时

    /// <summary>
    /// 创建战斗
    /// </summary>
    /// <param name="_battleID">战斗唯一ID</param>
    /// <param name="_battleUser">参与战斗的玩家列表</param>
    public void CreatBattle(int _battleID, List<MatchUserInfo> _battleUser)
    {
        // 帧同步要保证每个客户端的计算结果一致，服务器产生随机数种子，发送给客户端使用
        int randSeed = UnityEngine.Random.Range(0, 100);

        // Thread.Sleep(500);
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
                userBattleID++;  // 为每个user设置一个battleID， 这里就从1开始。

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

    public void DestroyBattle()
    {
        foreach (var item in dic_udp.Values)
        {
            item.EndClientUdp();
        }

        _isRun = false;
    }

    private void FinishBattle()
    {
        foreach (var item in dic_udp.Values)
        {
            item.EndClientUdp();
        }

        BattleManage.Instance.FinishBattle(battleID);
    }

    private void CheckBattleBegin(int _userBattleID)
    {

        if (isBeginBattle)
        {
            return;
        }

        dic_battleReady[_userBattleID] = true;

        isBeginBattle = true;
        //	Debug.Log("dic_battleReady.Count  == " + dic_battleReady.Count);
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

        Thread _threadSenfd = new Thread(Thread_SendFrameData);
        _threadSenfd.Start();
    }


    private void Thread_SendFrameData()
    {
        //向玩家发送战斗开始
        bool isFinishBS = false;
        Debug.Log("Thread_SendFrameData dic_udp.Count****  " + dic_udp.Count);
        while (!isFinishBS)
        {
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
                    Debug.Log("Thread_SendFrameData 没有收到全部玩家的第一帧数据 ****  i= =" + i);
                    _allData = false;// 有一个玩家没有发送上来操作 则判断为false
                    break;
                }
            }


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

        while (_isRun)
        {
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

            Thread.Sleep(ServerConfig.frameTime);
        }

        Debug.Log("帧数据发送线程结束.....................");
    }

    public void UpdatePlayerOperation(PlayerOperation _operation, int _mesNum)
    {


        int _index = _operation.battleID - 1;
        // Debug.Log("收到玩家操作:" + _index + "," + _mesNum + "," + playerMesNum[_index]); // mesnum 客户端消息序列，   playuermesnum 服务器存储的玩家消息。
        //		Debug.Log ("收到玩家操作:" + _index + "," + _mesNum + "," + playerMesNum [_index]);
        if (_mesNum > playerMesNum[_index])
        { // 如果消息ID小于客户端的，则服务器丢帧，赋值补足。
            frameOperation[_index] = _operation;
            playerMesNum[_index] = _mesNum;
        }
        else
        {
            //早期的包就不记录了
        }
    }

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
            //			Debug.Log ("战斗即将结束咯......");
            _isRun = false;

            finishTime = 2000f;
            if (waitBattleFinish == null)
            {
                waitBattleFinish = new Timer(new TimerCallback(WaitClientFinish), null, 1000, 1000);
            }
        }
    }

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

    public void AnalyzeMessage(CSID messageId, byte[] bodyData)
    {
        Debug.Log("AnalyzeMessage   messageId == " + messageId);
        switch (messageId)
        {
            case CSID.UDP_BATTLE_READY:
                {
                    //接收战斗准备
                    UdpBattleReady _mes = CSData.DeserializeData<UdpBattleReady>(bodyData);
                    Debug.Log("pb_ReceiveMes.mesID == " + _mes.uid + "  接收战斗准备 ");
                    CheckBattleBegin(_mes.battleID);
                    Debug.Log("pb_ReceiveMes.mesID2 *** ");
                    dic_udp[_mes.battleID].RecvClientReady(_mes.uid);
                }
                break;
            case CSID.UDP_UP_PLAYER_OPERATIONS:
                {
                    UdpUpPlayerOperations pb_ReceiveMes = CSData.DeserializeData<UdpUpPlayerOperations>(bodyData);
                    //     Debug.Log("pb_ReceiveMes.mesID == " + pb_ReceiveMes.mesID + "  move " + pb_ReceiveMes.operation.move) ;
                    // Debug.Log("pb_ReceiveMes.mesID == " + pb_ReceiveMes.mesID + "  move " + pb_ReceiveMes.operation.move);
                    UpdatePlayerOperation(pb_ReceiveMes.operation, pb_ReceiveMes.mesID);
                }
                break;
            case CSID.UDP_UP_DELTA_FRAMES:
                {
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
