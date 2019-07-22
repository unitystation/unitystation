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
		//subscribe to when the UI element is modified so we can set the canister's actual release pressure.
		ReleasePressureDial.OnServerValueSet.AddListener(ServerUpdateReleasePressure);
	}

	private void ServerUpdateReleasePressure(int newValue)
	{
		container.ReleasePressure = newValue;
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