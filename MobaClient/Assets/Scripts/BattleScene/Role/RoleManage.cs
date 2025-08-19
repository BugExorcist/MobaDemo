using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PBBattle;
public class RoleManage:MonoBehaviour {

	public bool initFinish;
	private GameObject pre_roleBase;
	private GameObject pre_roleUI;

	private Transform roleParent;
	private Dictionary<int,RoleBase> dic_role;
//	void Start () {
//		
//	}

	public void InitData(Transform _roleParent,out GameVector2[] roleGrid){
		initFinish = false;
 

		int _roleNum = BattleData.Instance.list_battleUser.Count;
		roleGrid = new GameVector2[_roleNum];
		 
		StartCoroutine (CreatRole(roleGrid));
	}

	IEnumerator CreatRole(GameVector2[] _roleGrid){

		yield return new WaitForEndOfFrame();
		initFinish = true;
	}

	public RoleBase GetRoleFromBattleID(int _id){
		return dic_role [_id];
	}

	 
	 
}
