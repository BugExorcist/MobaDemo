using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MarsClickBtn : MonoBehaviour {
	private EventTrigger _EventTri;
	private Image btnImage;
	void Start () {
		_EventTri = GetComponent<EventTrigger> ();
		btnImage = GetComponent<Image> ();
	}
	
	public void EnableButton(){ 
	}

	public void DisableButton(){ 
	}

	public void OnClickDown(){
	 

	}

	public void OnClickUp(){ 
	}
}
