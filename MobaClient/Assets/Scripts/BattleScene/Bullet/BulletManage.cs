using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletManage : MonoBehaviour {
	
	public bool initFinish;
	private int bulletID;
	private Transform bulletParent;
	private GameObject prefabBullet;

	private Dictionary<int,BulletBase> dic_bullets;
	private List<BulletBase> list_destoryBullet;
	public void InitData(Transform _bulletParent){
		initFinish = false;
		 
		StartCoroutine (LoadBullet());
	}


	IEnumerator LoadBullet(){
	 
		yield return new WaitForEndOfFrame ();
		initFinish = true;
	}
	 
}
