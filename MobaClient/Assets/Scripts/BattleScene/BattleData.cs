using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PBBattle;

/// <summary>
/// 战斗数据管理类
/// 负责存储和处理战斗相关的数据，采用单例模式设计
/// </summary>
public class BattleData
{
    #region 战斗基础数据
    /// <summary>
    /// 随机种子
    /// 由服务器生成，确保所有客户端计算结果一致
    /// </summary>
    public int randSeed;
    
    /// <summary>
    /// 当前玩家的战斗ID
    /// </summary>
    public int battleID;

    #region 地图相关常量
    /// <summary>
    /// 地图行数
    /// </summary>
    public const int mapRow = 7;
    
    /// <summary>
    /// 地图列数
    /// </summary>
    public const int mapColumn = 13;
    
    /// <summary>
    /// 格子的逻辑大小
    /// </summary>
    public const int gridLenth = 10000;
    
    /// <summary>
    /// 格子逻辑大小的一半
    /// </summary>
    public const int gridHalfLenth = 5000;
    #endregion

    /// <summary>
    /// 地图总格子数
    /// </summary>
    public int mapTotalGrid;
    
    /// <summary>
    /// 地图宽度
    /// </summary>
    public int mapWidth;
    
    /// <summary>
    /// 地图高度
    /// </summary>
    public int mapHeigh;
    #endregion

    #region 玩家和操作数据
    /// <summary>
    /// 战斗玩家信息列表
    /// </summary>
    public List<BattleUserInfo> list_battleUser;
    
    /// <summary>
    /// 移动方向与速度映射字典
    /// key: 方向
    /// value: 对应方向的速度向量
    /// </summary>
    private Dictionary<int, GameVector2> dic_speed;

    /// <summary>
    /// 当前操作ID
    /// 用于标识玩家操作的序号
    /// </summary>
    private int curOperationID;
    
    /// <summary>
    /// 玩家自身操作数据
    /// </summary>
    public PlayerOperation selfOperation;

    /// <summary>
    /// 当前帧ID
    /// </summary>
    private int curFramID;
    
    /// <summary>
    /// 最大帧ID
    /// </summary>
    private int maxFrameID;
    
    /// <summary>
    /// 最大发送帧数
    /// 一次最多请求的缺失帧数
    /// </summary>
    private int maxSendNum;

    /// <summary>
    /// 缺失的帧列表
    /// 记录未接收到的帧ID
    /// </summary>
    private List<int> lackFrame;
    
    /// <summary>
    /// 帧数据字典
    /// key: 帧ID
    /// value: 该帧的所有玩家操作
    /// </summary>
    private Dictionary<int, AllPlayerOperation> dic_frameDate;
    
    /// <summary>
    /// 玩家操作ID字典
    /// key: 玩家战斗ID
    /// value: 最新有效的操作ID
    /// </summary>
    private Dictionary<int, int> dic_rightOperationID;
    #endregion

    #region 统计数据
    /// <summary>
    /// 帧率
    /// </summary>
    public int fps;
    
    /// <summary>
    /// 网络包数量
    /// </summary>
    public int netPack;
    
    /// <summary>
    /// 发送数量
    /// </summary>
    public int sendNum;
    
    /// <summary>
    /// 接收数量
    /// </summary>
    public int recvNum;
    #endregion

    #region 单例模式
    /// <summary>
    /// 单例实例
    /// </summary>
    private static BattleData instance;
    
    /// <summary>
    /// 单例访问属性
    /// 提供全局访问点
    /// </summary>
    public static BattleData Instance
    {
        get
        {
            // 如果类的实例不存在则创建，否则直接返回
            if (instance == null)
            {
                instance = new BattleData();
            }
            return instance;
        }
    }
    #endregion

    #region 构造和初始化
    /// <summary>
    /// 私有构造函数
    /// 防止外部直接实例化
    /// </summary>
    private BattleData()
    {
        mapTotalGrid = mapRow * mapColumn;
        mapWidth = mapColumn * gridLenth;
        mapHeigh = mapRow * gridLenth;

        curOperationID = 1;
        selfOperation = new PlayerOperation();
        selfOperation.move = 121;
        ResetRightOperation();

        dic_speed = new Dictionary<int, GameVector2>();
        //初始化速度表
        GlobalData.Instance().GetFileStringFromStreamingAssets("Desktopspeed.txt", _fileStr =>
        {
            InitSpeedInfo(_fileStr);
        });

        curFramID = 0;
        maxFrameID = 0;
        maxSendNum = 5;

        lackFrame = new List<int>();
        dic_rightOperationID = new Dictionary<int, int>();
        dic_frameDate = new Dictionary<int, AllPlayerOperation>();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 更新战场信息
    /// </summary>
    /// <param name="_randseed">随机数种子</param>
    /// <param name="_userInfo">用户列表</param>
    public void UpdateBattleInfo(int _randseed, List<BattleUserInfo> _userInfo)
    {
        Debug.Log("UpdateBattleInfo  更新战场信息 " + Time.realtimeSinceStartup);
        randSeed = _randseed;
        list_battleUser = new List<BattleUserInfo>(_userInfo);

        foreach (var item in list_battleUser)
        {
            if (item.uid == NetGlobal.Instance().userUid)
            {
                battleID = item.battleID;
                selfOperation.battleID = battleID;
                Debug.Log("自己的battleId:" + battleID);
            }

            dic_rightOperationID[item.battleID] = 0;
        }
    }

    /// <summary>
    /// 清除数据
    /// 重置战斗数据到初始状态
    /// </summary>
    public void ClearData()
    {
        curOperationID = 1;
        selfOperation.move = 121;
        ResetRightOperation();

        curFramID = 0;
        maxFrameID = 0;
        maxSendNum = 5;

        lackFrame.Clear();
        dic_rightOperationID.Clear();
        dic_frameDate.Clear();
    }

    /// <summary>
    /// 销毁实例
    /// 释放资源并将单例置空
    /// </summary>
    public void Destory()
    {
        list_battleUser.Clear();
        list_battleUser = null;
        instance = null;
    }

    /// <summary>
    /// 初始化速度信息
    /// 从配置文件中加载不同方向的速度数据
    /// </summary>
    /// <param name="_fileStr">配置文件内容</param>
    void InitSpeedInfo(string _fileStr)
    {
        string[] lineArray = _fileStr.Split("\n"[0]);

        int dir;
        for (int i = 0; i < lineArray.Length; i++)
        {
            if (lineArray[i] != "")
            {
                GameVector2 date = new GameVector2();
                string[] line = lineArray[i].Split(new char[1] { ',' }, 3);
                dir = System.Int32.Parse(line[0]);
                date.x = System.Int32.Parse(line[1]);
                date.y = System.Int32.Parse(line[2]);
                dic_speed[dir] = date;
            }
        }
    }

    /// <summary>
    /// 获取指定方向的速度
    /// </summary>
    /// <param name="_dir">方向</param>
    /// <returns>速度向量</returns>
    public GameVector2 GetSpeed(int _dir)
    {
        return dic_speed[_dir];
    }

    /// <summary>
    /// 确保坐标不超出地图范围
    /// </summary>
    /// <param name="_pos">原始坐标</param>
    /// <returns>限制后的坐标</returns>
    public GameVector2 GetMapLogicPosition(GameVector2 _pos)
    {
        return new GameVector2(Mathf.Clamp(_pos.x, 0, mapWidth), Mathf.Clamp(_pos.y, 0, mapHeigh));
    }

    /// <summary>
    /// 获取地图格子中心点坐标
    /// </summary>
    /// <param name="_row">行索引</param>
    /// <param name="_column">列索引</param>
    /// <returns>格子中心点坐标</returns>
    public GameVector2 GetMapGridCenterPosition(int _row, int _column)
    {
        return new GameVector2(_column * gridLenth + gridHalfLenth, _row * gridLenth + gridHalfLenth);
    }

    /// <summary>
    /// 根据随机数获取地图格子
    /// </summary>
    /// <param name="_randNum">随机数</param>
    /// <returns>格子坐标(行,列)</returns>
    public GameVector2 GetMapGridFromRand(int _randNum)
    {
        int _num1 = _randNum % mapTotalGrid;
        int _row = _num1 / mapColumn;
        int _column = _num1 % mapColumn;
        return new GameVector2(_row, _column);
    }

    /// <summary>
    /// 根据随机数获取地图格子中心点坐标
    /// </summary>
    /// <param name="_randNum">随机数</param>
    /// <returns>格子中心点坐标</returns>
    public GameVector2 GetMapGridCenterPositionFromRand(int _randNum)
    {
        GameVector2 grid = GetMapGridFromRand(_randNum);
        return GetMapGridCenterPosition(grid.x, grid.y);
    }

    /// <summary>
    /// 更新移动方向
    /// </summary>
    /// <param name="_dir">方向</param>
    public void UpdateMoveDir(int _dir)
    {
        // Debug.Log("_dir  ************   "  + _dir);
        selfOperation.move = _dir;
    }

    /// <summary>
    /// 更新右侧操作
    /// </summary>
    /// <param name="_type">操作类型</param>
    /// <param name="_value1">操作值1</param>
    /// <param name="_value2">操作值2</param>
    public void UpdateRightOperation(RightOpType _type, int _value1, int _value2)
    {
        selfOperation.rightOperation = _type;
        selfOperation.operationValue1 = _value1;
        selfOperation.operationValue2 = _value2;
        selfOperation.operationID = curOperationID;
    }

    /// <summary>
    /// 检查操作ID是否有效
    /// </summary>
    /// <param name="_battleID">玩家战斗ID</param>
    /// <param name="_rightOpID">操作ID</param>
    /// <returns>是否有效</returns>
    public bool IsValidRightOp(int _battleID, int _rightOpID)
    {
        return _rightOpID > dic_rightOperationID[_battleID];
    }

    /// <summary>
    /// 更新右侧操作ID
    /// </summary>
    /// <param name="_battleID">玩家战斗ID</param>
    /// <param name="_opID">操作ID</param>
    /// <param name="_type">操作类型</param>
    public void UpdateRightOperationID(int _battleID, int _opID, RightOpType _type)
    {
        dic_rightOperationID[_battleID] = _opID;
        if (battleID == _battleID)
        {
            //玩家自己
            curOperationID++;
            if (_type == selfOperation.rightOperation)
            {
                ResetRightOperation();
            }
        }
    }

    /// <summary>
    /// 重置右侧操作
    /// 将右侧操作设置为无操作状态
    /// </summary>
    public void ResetRightOperation()
    {
        selfOperation.rightOperation = RightOpType.noop;
        selfOperation.operationValue1 = 0;
        selfOperation.operationValue2 = 0;
        selfOperation.operationID = 0;
    }

    /// <summary>
    /// 返回当前帧数据数量
    /// </summary>
    public int GetFrameDataNum()
    {
        if (dic_frameDate == null)
        {
            return 0;
        }
        else
        {
            return dic_frameDate.Count;
        }
    }

    /// <summary>
    /// 添加新帧数据
    /// </summary>
    /// <param name="_frameID">帧ID</param>
    /// <param name="_op">所有玩家操作</param>
    public void AddNewFrameData(int _frameID, AllPlayerOperation _op)
    {
        dic_frameDate[_frameID] = _op;
        for (int i = maxFrameID + 1; i < _frameID; i++)
        {
            lackFrame.Add(i);
            Debug.Log("缺失 :" + i);
        }
        maxFrameID = _frameID;

        //发送缺失帧数据
        if (lackFrame.Count > 0)
        {
            if (lackFrame.Count > maxSendNum)
            {
                List<int> sendList = lackFrame.GetRange(0, maxSendNum);
                UdpPB.Instance().SendDeltaFrames(selfOperation.battleID, sendList);
            }
            else
            {
                UdpPB.Instance().SendDeltaFrames(selfOperation.battleID, lackFrame);
            }
        }
    }

    /// <summary>
    /// 添加缺失的帧数据
    /// </summary>
    /// <param name="_frameID">帧ID</param>
    /// <param name="_newOp">帧操作数据</param>
    public void AddLackFrameData(int _frameID, AllPlayerOperation _newOp)
    {
        //删除缺失的帧记录
        if (lackFrame.Contains(_frameID))
        {
            dic_frameDate[_frameID] = _newOp;
            lackFrame.Remove(_frameID);
            Debug.Log("补上 :" + _frameID);
        }
    }

    /// <summary>
    /// 尝试获取下一帧玩家操作
    /// </summary>
    /// <param name="_op">输出参数，下一帧操作数据</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetNextPlayerOp(out AllPlayerOperation _op)
    {
        int _frameID = curFramID + 1;
        return dic_frameDate.TryGetValue(_frameID, out _op);
    }

    /// <summary>
    /// 操作执行成功
    /// 递增当前帧ID
    /// </summary>
    public void RunOpSucces()
    {
        curFramID++;
    }
    #endregion
}
