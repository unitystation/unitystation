using System;
using System.Collections;
using System.Collections.Generic;
using Learning;
using UnityEngine;

[CreateAssetMenu(fileName = "Alert", menuName = "ScriptableObjects/UIAlerts")]
public class AlertSO : ScriptableObject
{

	public SpriteDataSO AssociatedSprite;

	public string HoverToolTip;

	public ProtipSO PlayerProtip;

	//conditional
	//hide if others are present
	//
	public List<AlertSO> DoNotShowIfPresent = new List<AlertSO>();

	// public bool Showpriority => DoNotShowIfPresent.Count > 0;
	//
	// [NaughtyAttributes.ShowIf("Showpriority")]
	// public int DoNotShowIfPresentPriority;


	[NonSerialized] public int SetID = -1;

	public int GetIndexed()
	{
		if (SetID == -1)
		{
			SetID = AlertSOs.Instance.AllAlertSOs.IndexOf(this);
		}
		return SetID;
	}

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
