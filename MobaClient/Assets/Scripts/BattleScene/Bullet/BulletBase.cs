using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBase : MonoBehaviour {
	[HideInInspector]
	public ShapeBase objShape;

//	private int owerID;
	public int speed;
	public int life;
	private int curLife;

	private Vector3 renderPosition;
	private GameVector2 logicSpeed;
	public void InitData(int _owerID,int _id,GameVector2 _logicPos,int _moveDir){
 
	}

	public int GetBulletID(){
		return objShape.ObjUid.objectID;
	}
	// Update is called once per frame
	void Update () {
		transform.position = Vector3.Lerp(transform.position,renderPosition,0.4f);
	}

	public virtual void Logic_Move(){
	 
	}

	public virtual void Logic_Collision(){
		 
	}
 
}
