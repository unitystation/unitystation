using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_P_Bool : MonoBehaviour
{
	public Toggle TToggle;

	public void SetState(bool thebool){
		TToggle.isOn = thebool;
	
	}
}
