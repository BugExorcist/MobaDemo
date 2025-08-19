using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PBBattle;
public class BattleCon : MonoBehaviour
{
    #region 事件委托
    /// <summary>
    /// 无参数的委托类型
    /// 用于定义各种战斗事件的回调函数
    /// </summary>
    public delegate void DelegateEvent();
    
    /// <summary>
    /// 准备完成事件
    /// 当战斗准备完成后触发，用于关闭对局等待界面
    /// </summary>
    public DelegateEvent delegate_readyOver;
    
    /// <summary>
    /// 游戏结束事件
    /// 当战斗结束后触发
    /// </summary>
    public DelegateEvent delegate_gameOver;
    #endregion

    #region 战斗状态变量
    /// <summary>
    /// 战斗是否已开始
    /// 防止战斗开始逻辑被重复执行
    /// </summary>
    private bool isBattleStart;
    
    /// <summary>
    /// 战斗是否已结束
    /// 标记战斗的结束状态
    /// </summary>
    private bool isBattleFinish;
    #endregion

    #region 管理器引用
    /// <summary>
    /// 角色管理器
    /// 负责管理战斗中的所有角色单位
    /// </summary>
    [HideInInspector]
    public RoleManage roleManage;
    
    /// <summary>
    /// 物理碰撞管理器
    /// 负责管理战斗场景中的障碍物和物理碰撞检测
    /// </summary>
    [HideInInspector]
    public ObstacleManage obstacleManage;
    
    /// <summary>
    /// 飞行道具管理器
    /// 负责管理战斗中的子弹、技能等飞行道具
    /// </summary>
    [HideInInspector]
    public BulletManage bulletManage;
    #endregion

    #region 单例模式
    /// <summary>
    /// 单例实例
    /// 存储BattleCon的唯一实例
    /// </summary>
    private static BattleCon instance;
    
    /// <summary>
    /// 单例访问属性
    /// 提供全局访问点
    /// </summary>
    public static BattleCon Instance
    {
        get
        {
            return instance;
        }
    }
    #endregion

    #region Unity 生命周期方法
    /// <summary>
    /// 初始化单例
    /// 在对象创建时调用
    /// </summary>
    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 启动战斗逻辑
    /// 在场景加载完成后调用
    /// 初始化UDP连接、注册网络事件、启动初始化等待协程
    /// </summary>
    void Start()
    {
        // 注册所有网络事件回调
        UdpPB.Instance().StartClientUdp();
        UdpPB.Instance().mes_battle_start = Message_Battle_Start;
        UdpPB.Instance().mes_frame_operation = Message_Frame_Operation;

        isBattleStart = false;
        StartCoroutine(WaitInitData());
    }

    /// <summary>
    /// 更新方法
    /// 每帧调用一次
    /// 处理玩家输入
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UdpPB.Instance().MyDestory();
        }
    }

    /// <summary>
    /// 销毁方法
    /// 当对象被销毁时调用
    /// 清理战斗数据和网络资源
    /// </summary>
    void OnDestroy()
    {
        BattleData.Instance.ClearData();
        UdpPB.Instance().Destory();
        instance = null;
    }
    #endregion


    /// <summary>
    /// 等待初始化数据完成
    /// 等待所有管理器初始化完成后开始发送战斗准备消息
    /// </summary>
    IEnumerator WaitInitData()
    {
        yield return new WaitUntil(() =>
        {
            // 等待直到初始化完成
            return roleManage.initFinish && obstacleManage.initFinish && bulletManage.initFinish;
        });

        // 0.5秒后每0.2秒发送一次战斗准备消息
        this.InvokeRepeating("Send_BattleReady", 0.5f, 0.2f);
    }


    #region 初始化方法
    /// <summary>
    /// 初始化战斗数据
    /// 创建并初始化各管理器
    /// </summary>
    /// <param name="_map">地图根节点</param>
    public void InitData(Transform _map)
    {
        ToolRandom.srand((ulong)BattleData.Instance.randSeed);
        roleManage = gameObject.AddComponent<RoleManage>();
        obstacleManage = gameObject.AddComponent<ObstacleManage>();
        bulletManage = gameObject.AddComponent<BulletManage>();

        GameVector2[] roleGrid;
        roleManage.InitData(_map.Find("Role"), out roleGrid);
        obstacleManage.InitData(_map.Find("Obstacle"), roleGrid);
        bulletManage.InitData(_map.Find("Bullet"));
    }
    #endregion

    #region 战斗逻辑方法
    void Message_Battle_Start(UdpBattleStart _mes)
    {
        BattleStart();
    }

    void BattleStart()
    {
        Debug.Log("BattleStart isBattleStart " + isBattleStart);
        if (isBattleStart)
        {
            return;
        }

        isBattleStart = true;
        this.CancelInvoke("Send_BattleReady");

        float _time = NetConfig.frameTime * 0.001f;  // 66ms
        this.InvokeRepeating("Send_operation", _time, _time);  // 循环调用 Send_operation 方法

        StartCoroutine("WaitForFirstMessage");
    }

    /// <summary>
    /// 发送战斗准备消息
    /// 向服务器发送战斗准备就绪通知
    /// </summary>
    void Send_BattleReady()
    {
        UdpPB.Instance().SendBattleReady(NetGlobal.Instance().userUid, BattleData.Instance.battleID);
    }

    /// <summary>
    /// 发送操作数据
    /// 向服务器发送玩家当前操作
    /// </summary>
    void Send_operation()
    {
        UdpPB.Instance().SendOperation();
    }

    /// <summary>
    /// 等待第一帧消息
    /// 等待服务器发送第一帧数据后开始逻辑更新
    /// </summary>
    IEnumerator WaitForFirstMessage()
    {
        yield return new WaitUntil(() =>
        {
            return BattleData.Instance.GetFrameDataNum() > 0; // 在这里等待第一帧，第一帧没更新之前不会做更新。
        });
        this.InvokeRepeating("LogicUpdate", 0f, 0.020f);

        delegate_readyOver?.Invoke();   // 关闭对局等待界面
    }

    /// <summary>
    ///  收到帧数据
    /// </summary>
    void Message_Frame_Operation(UdpDownFrameOperations _mes)
    {
        BattleData.Instance.AddNewFrameData(_mes.frameID, _mes.operations);
        BattleData.Instance.netPack++;
    }

    //逻辑帧更新
    void LogicUpdate()
    {

    }
    #endregion
}

