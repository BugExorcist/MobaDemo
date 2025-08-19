using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObstacleManage : MonoBehaviour {

	public bool initFinish;

	private int obstacleID;
	private Transform obstacleParent;
	private Dictionary<string,GameObject> dic_preObstacles;
	private Dictionary<int,ObstacleBase> dic_obstacles;

	private List<ObstacleBase> list_brokenObs;
	public void InitData(Transform _obstParent,GameVector2[] _roleGrid){
		initFinish = false;
	 
        initFinish = true;
    }

    

	public bool CollisionCorrection(GameVector2 _tVec,int _tRadius,out GameVector2 _tVecCC){
		bool _result = false;

		_tVecCC = _tVec;
		foreach (var item in dic_obstacles.Values) {
			if (!item.objShape.IsInBaseCircleDistance (_tVecCC,_tRadius)) {
				continue;
			}

			GameVector2 _amend;
			if (item.objShape.IsCollisionCircleCorrection (_tVecCC, _tRadius, out _amend)) {
				_tVecCC += _amend;
				_result = true;
			}
		}
		return _result;
	}

	 

}
