using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FieldGenerator : InputTrigger, IElectricalNeedUpdate, IDeviceControl
{
	public PoweredDevice poweredDevice;
	private bool SelfDestruct = false;
	[SyncVar(hook = "CheckState")]
	public bool isOn = false;
	public bool connectedToOther = false;
	private Coroutine coSpriteAnimator;
	public Sprite offSprite;
	public Sprite onSprite;
	public Sprite[] searchingSprites;
	public Sprite[] connectedSprites;
	public SpriteRenderer spriteRend;
	List<Sprite> animSprites = new List<Sprite>();
	public float Voltage;
	public float Resistance = 1500;
	public float PreviousResistance = 1500;
	private Resistance resistance = new Resistance();
	public PowerTypeCategory ApplianceType = PowerTypeCategory.FieldGenerator;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.StandardCable
	};

	public override void OnStartServer()
	{
		base.OnStartServer();
		poweredDevice.InData.CanConnectTo = CanConnectTo;
		poweredDevice.InData.Categorytype = ApplianceType;
		poweredDevice.DirectionStart = 0;
		poweredDevice.DirectionEnd = 9;
		resistance.Ohms = Resistance;
		ElectricalSynchronisation.PoweredDevices.Add(this);
		PowerInputReactions PRLCable = new PowerInputReactions();
		PRLCable.DirectionReaction = true;
		PRLCable.ConnectingDevice = PowerTypeCategory.StandardCable;
		PRLCable.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PRLCable.DirectionReactionA.YouShallNotPass = true;
		PRLCable.ResistanceReaction = true;
		PRLCable.ResistanceReactionA.Resistance = resistance;
		poweredDevice.InData.ConnectionReaction[PowerTypeCategory.StandardCable] = PRLCable;
		poweredDevice.InData.ControllingDevice = this;
		poweredDevice.InData.ControllingUpdate = this;

	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		CheckState(isOn);
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}
		else
		{
			isOn = !isOn;
			CheckState(isOn);
		}

		return true;
	}

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	public void PowerUpdateStructureChange() { }
	public void PowerUpdateStructureChangeReact() { }
	public void InitialPowerUpdateResistance() {
		foreach (KeyValuePair<IElectricityIO,HashSet<PowerTypeCategory>> Supplie in poweredDevice.Data.ResistanceToConnectedDevices) {
			poweredDevice.ResistanceInput(ElectricalSynchronisation.currentTick, 1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add (Supplie.Key.InData.ControllingUpdate);
		}
	}
	public void PowerUpdateResistanceChange() { 
		foreach (KeyValuePair<IElectricityIO,HashSet<PowerTypeCategory>> Supplie in poweredDevice.Data.ResistanceToConnectedDevices) {
			poweredDevice.ResistanceInput(ElectricalSynchronisation.currentTick, 1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add (Supplie.Key.InData.ControllingUpdate);
		}

	}
	public void PowerUpdateCurrentChange()
	{

	}
	public void PowerNetworkUpdate()
	{
		if (Resistance != PreviousResistance)
		{
			PreviousResistance = Resistance;
			resistance.Ohms = Resistance;
			ElectricalSynchronisation.ResistanceChange.Add (this);
		}
		Voltage = poweredDevice.Data.ActualVoltage;
		UpdateSprites ();
		//Logger.Log (Voltage.ToString () + "yeaahhh")   ;
	}

	void UpdateSprites(){
		if (isOn)
		{
			if(Voltage < 2700){
				if (coSpriteAnimator != null) {
					StopCoroutine(coSpriteAnimator);
					coSpriteAnimator = null;
				}
				spriteRend.sprite = onSprite;
			}
			if(Voltage >= 2700){
				if(!connectedToOther){
					animSprites = new List<Sprite>(searchingSprites);
					if (coSpriteAnimator == null) {
						coSpriteAnimator = StartCoroutine(SpriteAnimator());
					}
				} else {
					animSprites = new List<Sprite>(connectedSprites);
					if(coSpriteAnimator == null) {
						coSpriteAnimator = StartCoroutine(SpriteAnimator());
					}
				}
			}
		}
		else
		{
			if (coSpriteAnimator != null)
			{
				StopCoroutine(coSpriteAnimator);
				coSpriteAnimator = null;
			}
			spriteRend.sprite = offSprite;
		}
	}
	//Check the operational state
	void CheckState(bool _isOn)
	{

	}

	IEnumerator SpriteAnimator()
	{
		int index = 0;
		while (true)
		{
			if (index >= animSprites.Count)
			{
				index = 0;
			}
			spriteRend.sprite = animSprites[index];
			index++;
			yield return new WaitForSeconds(0.3f);
		}
	}
	public void OnDestroy()
	{
//		ElectricalSynchronisation.StructureChangeReact = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		ElectricalSynchronisation.PoweredDevices.Remove(this);
		SelfDestruct = true;
		//Make Invisible
	}
	public void TurnOffCleanup (){
	}
}