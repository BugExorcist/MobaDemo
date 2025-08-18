using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BattleManage
{

    private int battleID;
    private Dictionary<int, BattleCon> dic_battles;

    private static BattleManage instance = null;
    public static BattleManage Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new BattleManage();
            }
            return instance;
        }
    }

    private BattleManage()
    {
        battleID = 0;
        dic_battles = new Dictionary<int, BattleCon>();
    }

    public void Creat()
    {

    }


    public void Destroy()
    {
        foreach (var item in dic_battles)
        {
            item.Value.DestroyBattle();
        }
        dic_battles.Clear();
        instance = null;
    }

    /// <summary>
    /// 开始一场战斗
    /// </summary>
    public void BeginBattle(List<MatchUserInfo> _battleUser)
    {
        battleID++;
        BattleCon _battle = new BattleCon();
        _battle.CreatBattle(battleID, _battleUser);

        dic_battles[battleID] = _battle;

        Debug.Log("开始战斗。。。。。" + battleID);
    }

    /// <summary>
    /// 结束一场战斗
    /// </summary>
    public void FinishBattle(int _battleID)
    {
        dic_battles.Remove(_battleID);
        Debug.Log("战斗结束。。。。。" + _battleID);
    }


}
