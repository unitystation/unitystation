using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Objects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_Canister : NetTab
{
	public Image BG;
	public Image InnerPanelBG;
	public Text LabelText;
	public NumberSpinner InternalPressureDial;
	public NumberSpinner ReleasePressureDial;
	public NetWheel ReleasePressureWheel;
	private GasContainer container;
	public GameObject EditReleasePressurePopup;
	public Image XButton;

	//LED stuff
	public Graphic Red;
	public Graphic Green;
	public Graphic Yellow;
	private static readonly Color RED_ACTIVE = DebugTools.HexToColor("FF1C00");
	private static readonly Color RED_INACTIVE = DebugTools.HexToColor("730000");
	private static readonly Color YELLOW_ACTIVE = DebugTools.HexToColor("E4FF02");
	private static readonly Color YELLOW_INACTIVE = DebugTools.HexToColor("5E5400");
	private static readonly Color GREEN_ACTIVE = DebugTools.HexToColor("02FF23");
	private static readonly Color GREEN_INACTIVE = DebugTools.HexToColor("005E00");
	private bool flashingRed;
	private float secondsSinceFlash;
	private static readonly float SECONDS_PER_FLASH = 0.3f;


	private static readonly float GreenLowerBound = 10 * AtmosConstants.ONE_ATMOSPHERE;
	private static readonly  float YellowLowerBound = 5 * AtmosConstants.ONE_ATMOSPHERE;
	private static readonly float RedLowerBound = 10f;

	public void OpenPopup()
	{
		EditReleasePressurePopup.SetActive(true);
		EditReleasePressurePopup.GetComponentInChildren<InputFieldFocus>().Select();
	}

	public void ClosePopup()
	{
		EditReleasePressurePopup.SetActive(false);
	}


	private void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(ClientWaitForProvider());
	}

	IEnumerator ClientWaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
		//set the tab color and label based on the provider
		var canister = Provider.GetComponent<Canister>();
		BG.color = canister.UIBGTint;
		InnerPanelBG.color = canister.UIInnerPanelTint;
		LabelText.text = "Contains\n" + canister.ContentsName;
		XButton.color = canister.UIBGTint;
		UpdateLEDs(InternalPressureDial.SyncedValue);
		InternalPressureDial.OnSyncedValueChanged.AddListener(UpdateLEDs);
	}

	private void UpdateLEDs(int pressure)
	{
		if (pressure > GreenLowerBound)
		{
			flashingRed = false;
			Red.color = RED_INACTIVE;
			Yellow.color = YELLOW_INACTIVE;
			Green.color = GREEN_ACTIVE;
		}
		else if (pressure > YellowLowerBound)
		{
			flashingRed = false;
			Red.color = RED_INACTIVE;
			Yellow.color = YELLOW_ACTIVE;
			Green.color = GREEN_INACTIVE;
		}
		else if (pressure > RedLowerBound)
		{
			//flashing red (if not already)
			if (!flashingRed)
			{
				flashingRed = true;
				Red.color = RED_ACTIVE;
				Yellow.color = YELLOW_INACTIVE;
				Green.color = GREEN_INACTIVE;
			}
		}
		else
		{
			//empty
			flashingRed = false;
			Red.color = RED_INACTIVE;
			Yellow.color = YELLOW_INACTIVE;
			Green.color = GREEN_INACTIVE;
		}
	}

	private void Update()
	{
		if (flashingRed)
		{
			secondsSinceFlash += Time.deltaTime;
			if (secondsSinceFlash >= SECONDS_PER_FLASH)
			{
				secondsSinceFlash = 0;
				var curColor = Red.color;
				if (curColor == RED_ACTIVE)
				{
					Red.color = RED_INACTIVE;
				}
				else
				{
					Red.color = RED_ACTIVE;
				}
			}
		}
	}

	protected override void InitServer()
	{
		StartCoroutine(ServerWaitForProvider());
	}

	//server side
	IEnumerator ServerWaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		container = Provider.GetComponent<GasContainer>();
		//init pressure dials
		InternalPressureDial.ServerSpinTo(Mathf.RoundToInt(container.ServerInternalPressure));
		ReleasePressureDial.ServerSpinTo(Mathf.RoundToInt(container.ReleasePressure));
		//init wheel
		ReleasePressureWheel.SetValue = Mathf.RoundToInt(container.ReleasePressure).ToString();
		StartCoroutine(ServerRefreshInternalPressure());
	}

	private IEnumerator ServerRefreshInternalPressure()
	{
		InternalPressureDial.ServerSpinTo(Mathf.RoundToInt(container.ServerInternalPressure));
		yield return WaitFor.Seconds(0.5F);
		StartCoroutine(ServerRefreshInternalPressure());
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

	/// <summary>
	/// So we can edit using the free text entry
	/// </summary>
	/// <param name="newValue"></param>
	public void ServerEditReleasePressure(string newValue)
	{
		if (string.IsNullOrEmpty(newValue)) return;
		var releasePressure = Convert.ToInt32(newValue);
		releasePressure = Mathf.Clamp(releasePressure, 0, Canister.MAX_RELEASE_PRESSURE);
		ServerUpdateReleasePressure(releasePressure);
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

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}