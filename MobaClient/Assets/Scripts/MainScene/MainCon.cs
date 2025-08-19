using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PBCommon;
using PBMatch;
public class MainCon : MonoBehaviour
{

    public GameObject waitMatchObj;

    void Start()
    {
        waitMatchObj.SetActive(false);

        // 注册接收到Service消息后的处理方法
        TcpPB.Instance().mes_request_match_result = Message_Reauest_Match_Result; // 请求对局
        TcpPB.Instance().mes_cancel_match_result = Message_Cancel_Match_Result;// 取消对局
        TcpPB.Instance().mes_enter_battle = Message_Enter_Battle;   // 进入战场
    }

    /// <summary>
    /// 点击匹配战斗
    /// </summary>
    public void OnClickMatch()
    {
        Debug.Log("开始匹配");
        //开始匹配 
        TcpRequestMatch _mes = new TcpRequestMatch();
        _mes.uid = NetGlobal.Instance().userUid;
        _mes.roleID = 1;
        MyTcp.Instance.SendMessage(CSData.GetSendMessage<TcpRequestMatch>(_mes, CSID.TCP_REQUEST_MATCH));
    }

    /// <summary>
    /// 点击取消匹配战斗
    /// </summary>
    public void OnCliclCancelMatch()
    {
        //取消匹配
        TcpCancelMatch _mes = new TcpCancelMatch();
        _mes.uid = NetGlobal.Instance().userUid;
        MyTcp.Instance.SendMessage(CSData.GetSendMessage<TcpCancelMatch>(_mes, CSID.TCP_CANCEL_MATCH));
    }

    /// <summary>
    /// 收到开始匹配消息 展示匹配等待界面
    /// </summary>
    void Message_Reauest_Match_Result(TcpResponseRequestMatch _result)
    {

        Debug.Log("Message_Reauest_Match_Result  " + Time.realtimeSinceStartup);
        waitMatchObj.SetActive(true);
    }

    /// <summary>
    /// 收到匹配取消消息 关闭匹配等待界面
    /// </summary>
    void Message_Cancel_Match_Result(TcpResponseCancelMatch _result)
    {
        waitMatchObj.SetActive(false);
    }

    /// <summary>
    /// 收到进入战斗消息
    /// </summary>
    void Message_Enter_Battle(PBBattle.TcpEnterBattle _mes)
    {
        Debug.Log(_mes.battleUserInfo + "进入战场" + Time.realtimeSinceStartup);
        BattleData.Instance.UpdateBattleInfo(_mes.randSeed, _mes.battleUserInfo);

        ClearSceneData.LoadScene(GameConfig.battleScene);// 切换战斗场景
    }


    void OnDestroy()
    {
        TcpPB.Instance().mes_request_match_result = null;
        TcpPB.Instance().mes_cancel_match_result = null;
        TcpPB.Instance().mes_enter_battle = null;
    }
}
