using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GUI_P_Class : MonoBehaviour
{
	public Button TButton;
	public Text TText;
	public ulong ID;
	public void RequestOpenBookOnPage() { 
		OpenPageValueNetMessage.Send(ID);
	}
}
