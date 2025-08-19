using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ShapeCircle))]
public class RoleBase : MonoBehaviour {

	public int moveSpeed;
	public int attackTime;//攻击时间间隔
	private int curAttackTime;
//	private GameObject uiObj;
	private Transform modleParent;

	private Vector3 renderPosition;  // 渲染位置
	private Quaternion renderDir;
	private GameVector2 logicSpeed;
	private int roleDirection;//角色朝向
	[HideInInspector]
	public ShapeBase objShape; // 角色的shapeBase
	public Animator ani;
	public int hp;
	public TextMesh hpText;
	public void InitData(GameObject _ui,GameObject _modle,int _roleID,GameVector2 _logicPos){
		 
	} 
	 

  
  
}
