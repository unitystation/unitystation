using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class APC : NetworkBehaviour, IElectricalNeedUpdate, IDeviceControl
{
	public PoweredDevice poweredDevice;

	Sprite[] loadedScreenSprites;

	public Sprite[] redSprites;
	public Sprite[] blueSprites;
	public Sprite[] greenSprites;

	public Sprite deadSprite;

	public SpriteRenderer screenDisplay;

	public dynamic dynamicVariable = 1;

	public List<LightSource> ListOfLights = new List<LightSource>();

	public List<LightSwitchTrigger> ListOfLightSwitchTriggers = new List<LightSwitchTrigger>();

	private bool batteryInstalled = true;
	private bool isScreenOn = true;

	private bool SelfDestruct = false;

	private int charge = 10; //charge percent
	private int displayIndex = 0; //for the animation

	private Coroutine coScreenDisplayRefresh;
	public float Voltage;

	public float Resistance = 240;
	public float PreviousResistance = 240;
	private Resistance resistance = new Resistance();

	public PowerTypeCategory ApplianceType = PowerTypeCategory.APC;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.LowMachineConnector
	};

	//green - fully charged and sufficient power from wire
	//blue - charging, sufficient power from wire
	//red - running off internal battery, not enough power from wire

	public override void OnStartClient()
	{
		base.OnStartClient();
		poweredDevice.InData.CanConnectTo = CanConnectTo;
		poweredDevice.InData.Categorytype = ApplianceType;
		poweredDevice.DirectionStart = 0;
		poweredDevice.DirectionEnd = 9;
		resistance.Ohms = Resistance;
		ElectricalSynchronisation.PoweredDevices.Add(this);
		PowerInputReactions PRLCable = new PowerInputReactions();
		PRLCable.DirectionReaction = true;
		PRLCable.ConnectingDevice = PowerTypeCategory.LowVoltageCable;
		PRLCable.DirectionReactionA.AddResistanceCall.Bool = true;
		PRLCable.DirectionReactionA.YouShallNotPass = true;
		PRLCable.ResistanceReaction = true;
		PRLCable.ResistanceReactionA.Resistance = resistance;
		poweredDevice.InData.ConnectionReaction[PowerTypeCategory.LowMachineConnector] = PRLCable;
		poweredDevice.InData.ControllingDevice = this;
	}

	private void OnDisable()
	{
		ElectricalSynchronisation.PoweredDevices.Remove(this);
		if (coScreenDisplayRefresh != null)
		{
			StopCoroutine(coScreenDisplayRefresh);
			coScreenDisplayRefresh = null;
		}
	}

	//Called whenever the PoweredDevice updates
	void SupplyUpdate()
	{
		UpdateDisplay();
	}

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	public void PowerUpdateStructureChange()
	{ }
	public void PowerUpdateStructureChangeReact()
	{ }
	public void PowerUpdateResistanceChange()
	{ }
	public void PowerUpdateCurrentChange()
	{

	}

	public void PowerNetworkUpdate()
	{
		if (Resistance != PreviousResistance)
		{
			PreviousResistance = Resistance;
			resistance.Ohms = Resistance;
			ElectricalSynchronisation.ResistanceChange = true;
			ElectricalSynchronisation.CurrentChange = true;
		}
		Voltage = poweredDevice.Data.ActualVoltage;
		UpdateLights();
		//Logger.Log (Voltage.ToString () + "yeaahhh")   ;
	}
	public void UpdateLights()
	{
		for (int i = 0; i < ListOfLightSwitchTriggers.Count; i++)
		{
			ListOfLightSwitchTriggers[i].PowerNetworkUpdate(Voltage);
		}
		for (int i = 0; i < ListOfLights.Count; i++)
		{
			ListOfLights[i].PowerLightIntensityUpdate(Voltage);
		}
	}
	void UpdateDisplay()
	{
		//			if (poweredDevice.suppliedElectricity.current == 0 && charge == 0){
		//				loadedScreenSprites = null; // dead
		//			}
		//			if (poweredDevice.suppliedElectricity.current > 10 && charge > 0 && charge < 98) {
		//				loadedScreenSprites = blueSprites;
		//			}
		//			if (poweredDevice.suppliedElectricity.current > 10 && charge >= 98) {
		//				loadedScreenSprites = greenSprites;
		//			}
		//			if (poweredDevice.suppliedElectricity.current < 10 && charge > 0) {
		//				loadedScreenSprites = redSprites;
		//			}
	}

	IEnumerator ScreenDisplayRefresh()
	{
		yield return new WaitForEndOfFrame();
		while (true)
		{
			if (loadedScreenSprites == null)
				screenDisplay.sprite = deadSprite;
			else
			{
				if (++displayIndex >= loadedScreenSprites.Length)
				{
					displayIndex = 0;
				}
				screenDisplay.sprite = loadedScreenSprites[displayIndex];
			}
			yield return new WaitForSeconds(3f);
		}
	}

	//FIXME: Objects at runtime do not get destroyed. Instead they are returned back to pool
	//FIXME: that also renderers IDevice useless. Please reassess
	public void OnDestroy()
	{
		ElectricalSynchronisation.StructureChangeReact = true;
		ElectricalSynchronisation.ResistanceChange = true;
		ElectricalSynchronisation.CurrentChange = true;
		ElectricalSynchronisation.PoweredDevices.Remove(this);
		SelfDestruct = true;
		//Making Invisible
	}
}