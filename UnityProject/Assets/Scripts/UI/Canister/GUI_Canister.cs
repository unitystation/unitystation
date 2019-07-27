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

	//LED stuff
	public NetColorChanger Red;
	public NetColorChanger Green;
	public NetColorChanger Yellow;
	private static readonly string RED_ACTIVE = "FF1C00";
	private static readonly string RED_INACTIVE = "730000";
	private static readonly string YELLOW_ACTIVE = "E4FF02";
	private static readonly string YELLOW_INACTIVE = "5E5400";
	private static readonly string GREEN_ACTIVE = "02FF23";
	private static readonly string GREEN_INACTIVE = "005E00";

	private static readonly float GreenLowerBound = 10 * AtmosConstants.ONE_ATMOSPHERE;
	private static readonly  float YellowLowerBound = 5 * AtmosConstants.ONE_ATMOSPHERE;



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
		//init wheel
		ReleasePressureWheel.SetValue = container.ReleasePressure.ToString();
		//init colors
		UpdateLEDs();
	}

	private void UpdateLEDs()
	{
		var pressure = container.ServerInternalPressure;
		if (pressure > GreenLowerBound)
		{
			Red.SetValue = RED_INACTIVE;
			Yellow.SetValue = YELLOW_INACTIVE;
			Green.SetValue = GREEN_ACTIVE;
		}
		else if (pressure > YellowLowerBound)
		{
			Red.SetValue = RED_INACTIVE;
			Yellow.SetValue = YELLOW_ACTIVE;
			Green.SetValue = GREEN_INACTIVE;
		}
		else
		{
			Red.SetValue = RED_ACTIVE;
			Yellow.SetValue = YELLOW_INACTIVE;
			Green.SetValue = GREEN_INACTIVE;
		}
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
		UpdateLEDs();
	}

	/// <summary>
	/// Open / close the release valve of the attached container
	/// </summary>
	/// <param name="isOpen"></param>
	public void ServerToggleRelease(bool isOpen)
	{
		container.Opened = isOpen;
		if (isOpen)
		{
			ChatRelay.Instance.AddToChatLogServer(new ChatEvent
			{
				channels = ChatChannel.Local,
				message = $"Canister releasing at {container.ReleasePressure}",
				position = container.transform.position,
				radius = 3f
			});
		}
	}

	//TODO: Provide a way to close the tab
	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}