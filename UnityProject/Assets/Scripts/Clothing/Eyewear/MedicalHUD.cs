using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Mirror;
using UnityEngine;

public class MedicalHUD : NetworkBehaviour, IHUD
{
	[field:SerializeField]
	public GameObject Prefab { get; set; }

	public GameObject InstantiatedGameObject { get; set; }


	private MedicalHUDHandler MedicalHUDHandler;

	public HealthStateController HealthStateController;

	public PlayerScript PlayerScript;

	public HUDHandler HUDHandler;

	[SyncVar(hook = nameof(SyncCurrentState))]
	public HealthSymbol CurrentState = HealthSymbol.Healthy;


	[SyncVar(hook = nameof(SyncHealthBarPercentage))]
	public HealthBarPercentage CurrentHealthBarPercentage = HealthBarPercentage.Full100;

	public void Awake()
	{
		PlayerScript =  this.GetComponentCustom<PlayerScript>();
		HealthStateController = this.GetComponentCustom<HealthStateController>();
		HUDHandler = this.GetComponentCustom<HUDHandler>();
		HealthStateController.ServerOverallHealthChange += SetNewHealthServer;
		HUDHandler.AddNewHud(this);
	}

	public void SetUp()
	{
		MedicalHUDHandler = InstantiatedGameObject.GetComponent<MedicalHUDHandler>();
		MedicalHUDHandler.IconSymbol.SetCatalogueIndexSprite((int)CurrentState);
		MedicalHUDHandler.BarIcon.SetCatalogueIndexSprite((int)CurrentHealthBarPercentage);


		var visibility = false;
		var ThisType = typeof(MedicalHUD);
		if (HUDHandler.CategoryEnabled.ContainsKey(ThisType)) //So if you join mid round you still have the HUD showing
		{
			visibility = HUDHandler.CategoryEnabled[ThisType];
		}
		MedicalHUDHandler.SetVisible(visibility);
	}


	public void SetVisible(bool Visible)
	{
		MedicalHUDHandler.SetVisible(Visible);
	}

	public void SetNewHealthServer(float newHealth)
	{
		var NewCurrentState = HealthSymbol.Healthy;
		var NewHealth = HealthBarPercentage.Full100;
		var HealthPercentage = 0f;
		if (newHealth != 0)
		{
			HealthPercentage = newHealth / HealthStateController.MaxHealth;
		}


		switch (HealthPercentage)
		{
			case > 1.25f:
				NewHealth = HealthBarPercentage.Full100;
				NewCurrentState = HealthSymbol.Buffed;
				break;
			case > 1f:
				NewHealth = HealthBarPercentage.Full100;
				NewCurrentState = HealthSymbol.Healthy;
				break;
			case > 0.93f:
				NewHealth = HealthBarPercentage.Damaged93;
				NewCurrentState = HealthSymbol.SlightlyHurt;
				break;
			case > 0.87f:
				NewHealth = HealthBarPercentage.Damaged87;
				NewCurrentState = HealthSymbol.SlightlyHurt;
				break;
			case > 0.81f:
				NewHealth = HealthBarPercentage.Damaged81;
				NewCurrentState = HealthSymbol.SlightlyHurt;
				break;
			case > 0.75f:
				NewHealth = HealthBarPercentage.Damaged75;
				NewCurrentState = HealthSymbol.SlightlyHurt;
				break;
			case > 0.68f:
				NewHealth = HealthBarPercentage.Damaged68;
				NewCurrentState = HealthSymbol.SomewhatHurt;
				break;
			case > 0.62f:
				NewHealth = HealthBarPercentage.Damaged62;
				NewCurrentState = HealthSymbol.SomewhatHurt;
				break;
			case > 0.56f:
				NewHealth = HealthBarPercentage.Damaged56;
				NewCurrentState = HealthSymbol.SomewhatHurt;
				break;
			case > 0.50f:
				NewHealth = HealthBarPercentage.Damaged50;
				NewCurrentState = HealthSymbol.SomewhatHurt;
				break;
			case > 0.43f:
				NewHealth = HealthBarPercentage.Damaged43;
				NewCurrentState = HealthSymbol.VeryHurtSomewhat;
				break;
			case > 0.37f:
				NewHealth = HealthBarPercentage.Damaged37;
				NewCurrentState = HealthSymbol.VeryHurtSomewhat;
				break;
			case > 0.31f:
				NewHealth = HealthBarPercentage.Damaged31;
				NewCurrentState = HealthSymbol.VeryHurtSomewhat;
				break;
			case > 0.25f:
				NewHealth = HealthBarPercentage.Damaged25;
				NewCurrentState = HealthSymbol.VeryHurtSomewhat;
				break;
			case > 0.18f:
				NewHealth = HealthBarPercentage.Damaged18;
				NewCurrentState = HealthSymbol.BarelyConscious;
				break;
			case > 0.125f:
				NewHealth = HealthBarPercentage.Damaged12_5;
				NewCurrentState = HealthSymbol.BarelyConscious;
				break;
			case > 0.065f:
				NewHealth = HealthBarPercentage.Damaged6_5;
				NewCurrentState = HealthSymbol.BarelyConscious;
				break;
			case > 0f:
				NewHealth = HealthBarPercentage.Damaged0;
				NewCurrentState = HealthSymbol.BarelyConscious;
				break;
			case > -0.5f:
				NewHealth = HealthBarPercentage.CriticalN50;
				NewCurrentState = HealthSymbol.Critical;
				break;
			default:
				NewHealth = HealthBarPercentage.CriticalN85;
				NewCurrentState = HealthSymbol.Critical;
				break;
		}

		if (HealthStateController.ConsciousState == ConsciousState.DEAD)
		{
			NewHealth = HealthBarPercentage.Dead;
			NewCurrentState = HealthSymbol.Defibrillatorble;
			if (PlayerScript.HasSoul == false)
			{
				NewCurrentState = HealthSymbol.NoSoul;
			}

		}


		SyncHealthBarPercentage(CurrentHealthBarPercentage, NewHealth);
		SyncCurrentState(CurrentState, NewCurrentState);
	}

	//connectionToClient

	public void SyncCurrentState(HealthSymbol oldHealth, HealthSymbol newHealth)
	{
		CurrentState = newHealth;
		MedicalHUDHandler.IconSymbol.SetCatalogueIndexSprite((int)CurrentState);

	}


	public void SyncHealthBarPercentage(HealthBarPercentage oldHealth, HealthBarPercentage newHealth)
	{
		CurrentHealthBarPercentage = newHealth;
		MedicalHUDHandler.BarIcon.SetCatalogueIndexSprite((int)CurrentHealthBarPercentage);
	}

	public void OnDestroy()
	{
		HUDHandler.RemoveHud(this);
	}


	public enum HealthSymbol
	{
		Buffed  = 0, //blue
		Healthy,// medical symbol
		SlightlyHurt, //dark green
		SomewhatHurt,
		VeryHurtSomewhat, //Orange
		BarelyConscious, //red, She barely living now, Trippidy tripping now
		Critical, //flashing
		Defibrillatorble, //that defibrillator one
		NoSoul, //Skull, you are Dead! no, you! Pow hAhA. You are dead, no big surprise
		Ill, //green
		XenoEgg //eggy //TODO

	}


	public enum HealthBarPercentage
	{
		Full100 = 0,
		Damaged93,
		Damaged87,
		Damaged81,
		Damaged75,
		Damaged68,
		Damaged62,
		Damaged56,
		Damaged50,
		Damaged43,
		Damaged37,
		Damaged31,
		Damaged25,
		Damaged18,
		Damaged12_5,
		Damaged6_5,
		Damaged0,
		CriticalN50,
		CriticalN85,
		Dead
	}

}
