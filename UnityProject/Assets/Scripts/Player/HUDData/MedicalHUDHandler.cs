using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MedicalHUDHandler : MonoBehaviour
{

	public SpriteHandler IconSymbol;

	public SpriteHandler BarIcon;

	public void SetVisible(bool Visible)
	{
		IconSymbol.SetActive(Visible);
		BarIcon.SetActive(Visible);
	}

}


