using System.Collections;
using System.Collections.Generic;
using UI;
using UI.Core.NetUI;
using UnityEngine;

public class GUI_Thruster : NetTab
{
	public NetSlider MassConsumption;
	public NetText_label DirectionLabel;
	public NetText_label NameLabel;

	private Thruster Thruster;


	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
	}

	private IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
		Thruster = Provider.GetComponent<Thruster>();
		if (IsMasterTab)
		{
			RefreshValues();
		}
	}


	public void SetUp()
	{
		Thruster.ThisThrusterDirectionClassification = Thruster.ThrusterDirectionClassification.Up;
		RefreshValues();
	}
	public void SetRight()
	{
		Thruster.ThisThrusterDirectionClassification = Thruster.ThrusterDirectionClassification.Right;
		RefreshValues();
	}
	public void SetLeft()
	{
		Thruster.ThisThrusterDirectionClassification = Thruster.ThrusterDirectionClassification.Left;
		RefreshValues();
	}
	public void SetDown()
	{
		Thruster.ThisThrusterDirectionClassification = Thruster.ThrusterDirectionClassification.Down;
		RefreshValues();
	}


	public void SetMassUse(float Value)
	{
		Thruster.MaxMolesUseda = Value * 10f;
		RefreshValues(false);
	}

	public void RefreshValues(bool UpdateSlider =true)
	{
		if (UpdateSlider)
		{
			MassConsumption.MasterSetValue(Mathf.RoundToInt(Thruster.MaxMolesUseda * 10).ToString());
		}

		NameLabel.MasterSetValue(Thruster.gameObject.ExpensiveName());
		DirectionLabel.MasterSetValue(Thruster.ThisThrusterDirectionClassification.ToString());
	}

	public void Close()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}
