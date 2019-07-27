using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Objects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_Canister : NetTab
{
	public NumberSpinner InternalPressureDial;
	public NumberSpinner ReleasePressureDial;
	public NetWheel ReleasePressureWheel;

	private GasContainer container;

	protected override void InitServer()
	{
		StartCoroutine(WaitForProvider());
	}

	//server side
	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		container = Provider.GetComponent<GasContainer>();
		//init pressure dials
		InternalPressureDial.ServerSpinTo(Mathf.RoundToInt(container.ServerInternalPressure));
		ReleasePressureDial.ServerSpinTo(Mathf.RoundToInt(container.ReleasePressure));
		//subscribe to pressure changes
		container.OnServerInternalPressureChange.AddListener(OnServerInternalPressureChange);
	}

	/// <summary>
	/// Update the actual release pressure and all the attached UI elements
	/// </summary>
	/// <param name="newValue"></param>
	public void ServerUpdateReleasePressure(int newValue)
	{
		container.ReleasePressure = newValue;
		ReleasePressureDial.ServerSpinTo(newValue);
		ReleasePressureWheel.SetValue = newValue.ToString();
	}

	/// <summary>
	/// Allows for adding / subtracting from release pressure
	/// </summary>
	/// <param name="offset"></param>
	public void ServerAdjustReleasePressure(int offset)
	{
		ServerUpdateReleasePressure(Mathf.RoundToInt(container.ReleasePressure + offset));
	}

	private void OnServerInternalPressureChange(float newVal)
	{
		InternalPressureDial.ServerSpinTo(Mathf.RoundToInt(newVal));
	}

	/// <summary>
	/// Open / close the release valve of the attached container
	/// </summary>
	/// <param name="isOpen"></param>
	public void ServerToggleRelease(bool isOpen)
	{
		container.Opened = isOpen;
	}

	//TODO: Provide a way to close the tab
	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}