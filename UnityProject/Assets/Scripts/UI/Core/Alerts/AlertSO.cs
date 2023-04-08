using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Alert", menuName = "ScriptableObjects/UIAlerts")]
public class AlertSO : ScriptableObject
{

	public SpriteDataSO AssociatedSprite;

	//conditional
	//hide if others are present
	//
	public List<AlertSO> DoNotShowIfPresent = new List<AlertSO>();

	// public bool Showpriority => DoNotShowIfPresent.Count > 0;
	//
	// [NaughtyAttributes.ShowIf("Showpriority")]
	// public int DoNotShowIfPresentPriority;


	public bool CheckConditionalsShow(List<AlertUIElement> LivingAlerts)
	{
		foreach (var present in LivingAlerts)
		{
			if (DoNotShowIfPresent.Contains(present.AlertSO))
			{
				return false;
			}
		}

		return true;
	}
}
