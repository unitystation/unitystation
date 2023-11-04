using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityHUDHandler : MonoBehaviour
{

	public SpriteHandler MindShieldImplant;

	public SpriteHandler RoleIcon;

	public SpriteHandler StatusIcon;


	public void SetVisible(bool Visible)
	{
		MindShieldImplant.SetActive(Visible);
		RoleIcon.SetActive(Visible);
		StatusIcon.SetActive(Visible);
	}


}
