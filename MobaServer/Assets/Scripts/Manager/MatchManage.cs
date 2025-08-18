using System.Collections;
using System.Collections.Generic;
using PBBattle;

public struct MatchUserInfo
{
    public int uid;
    public int roleID;

    public MatchUserInfo(int _uid, int _roleID)
    {
        uid = _uid;
        roleID = _roleID;
    }
}

/// <summary>
/// 战斗匹配管理器
/// </summary>
public class MatchManage
{

    private List<MatchUserInfo> list_matchQueue;// 匹配玩家列表

    private static readonly object mmlockObj = new object();
    private static MatchManage instance = null;
    public static MatchManage Instance
    {
        get
        {
            lock (mmlockObj)
            {
                if (instance == null)
                {
                    instance = new MatchManage();
                }
            }
            return instance;
        }
    }

    private MatchManage()
    {
        list_matchQueue = new List<MatchUserInfo>();
    }

    public void Creat()
    {

    }


    public void Destroy()
    {
        instance = null;
    }

    /// <summary>
    /// 新增匹配用户
    /// </summary>
    public void NewMatchUser(int _uid, int _roleId)
    {
        list_matchQueue.Add(new MatchUserInfo(_uid, _roleId));// TODO:线程安全

        if (list_matchQueue.Count >= ServerConfig.battleUserNum)
        {   // 判断当前队列人数大于房间开启人数
            List<MatchUserInfo> _matchSureUser = new List<MatchUserInfo>();
            for (int i = 0; i < ServerConfig.battleUserNum; i++)
            {
                _matchSureUser.Add(list_matchQueue[0]);
                list_matchQueue.RemoveAt(0);
            }

            ServerGlobal.Instance.AddAction(() =>
            {
                BattleManage.Instance.BeginBattle(_matchSureUser);
            });
        }
    }

    public void CancleMatch(int _uid)
    {
        //		if (list_matchQueue.Contains(_uid)) {
        //			list_matchQueue.Remove (_uid);
        //		}

        for (int i = 0; i < list_matchQueue.Count; i++)
        {
            if (list_matchQueue[i].uid == _uid)
            {
                list_matchQueue.RemoveAt(i);
                break;
            }
        }
    }

    public void MatchSure(int _matchID, int _uid)
    {

    }

    public void CancleMatchSure(int _matchID, int _uid)
    {

    }
}
