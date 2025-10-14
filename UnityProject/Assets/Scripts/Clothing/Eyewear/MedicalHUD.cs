using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using HealthV2.Sickness;
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
		if (CustomNetworkManager.IsServer)
		{
			HealthStateController.ServerOverallHealthChange += SetNewHealthServer;
		}

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

		int currentStage = GetCurrentSicknessStage();
		switch (currentStage)
		{
			case 0:
				NewCurrentState = HealthSymbol.Healthy;
				break;
			case 1:
				NewCurrentState = HealthSymbol.SlightlyIll;
				break;
			case 2:
				NewCurrentState = HealthSymbol.ModeratelyIll;
				break;
			case 3:
				NewCurrentState = HealthSymbol.SubstantiouslyIll;
				break;
			case >4:
				NewCurrentState = HealthSymbol.HeavilyIll;
				break;
		}

		switch (HealthPercentage)
		{
			case > 1.25f:
				NewHealth = HealthBarPercentage.Full100;
				NewCurrentState = HealthSymbol.Buffed;
				break;
			case > 1f:
				NewHealth = HealthBarPercentage.Full100;
				break;
			case > 0.93f:
				NewHealth = HealthBarPercentage.Damaged93;
				break;
			case > 0.87f:
				NewHealth = HealthBarPercentage.Damaged87;
				break;
			case > 0.81f:
				NewHealth = HealthBarPercentage.Damaged81;
				break;
			case > 0.75f:
				NewHealth = HealthBarPercentage.Damaged75;
				break;
			case > 0.68f:
				NewHealth = HealthBarPercentage.Damaged68;
				break;
			case > 0.62f:
				NewHealth = HealthBarPercentage.Damaged62;
				break;
			case > 0.56f:
				NewHealth = HealthBarPercentage.Damaged56;
				break;
			case > 0.50f:
				NewHealth = HealthBarPercentage.Damaged50;
				break;
			case > 0.43f:
				NewHealth = HealthBarPercentage.Damaged43;
				break;
			case > 0.37f:
				NewHealth = HealthBarPercentage.Damaged37;
				break;
			case > 0.31f:
				NewHealth = HealthBarPercentage.Damaged31;
				break;
			case > 0.25f:
				NewHealth = HealthBarPercentage.Damaged25;
				break;
			case > 0.18f:
				NewHealth = HealthBarPercentage.Damaged18;
				break;
			case > 0.125f:
				NewHealth = HealthBarPercentage.Damaged12_5;
				break;
			case > 0.065f:
				NewHealth = HealthBarPercentage.Damaged6_5;
				break;
			case > 0f:
				NewHealth = HealthBarPercentage.Damaged0;
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

	private int GetCurrentSicknessStage()
	{
		int stage = 0;

		ReagentPoolSystem system = PlayerScript.playerHealth.reagentPoolSystem;
		if (system == null) return stage;

		ReagentMix blood = system.BloodPool;
		foreach (var cure in CureManager.Instance.CureableSicknesses)
		{
			if (blood.reagents.TryGetValue(cure.Sickness, out float amount) == false) continue;
			if (CommonSicknesses.Instance.diseaseReactionDictionary.TryGetValue(cure.Sickness.Name, out var reaction) == false) continue;

			float concentrationPercent = (amount / blood.Total) * 100;
			int newStage = reaction.GetStageID(concentrationPercent);
			if (newStage > stage) stage = newStage;
		}

		return stage;
	}

	public enum HealthSymbol
	{
		Buffed  = 0, //blue
		Healthy = 1,// medical symbol
		SlightlyIll = 2, //dark green
		ModeratelyIll = 3,
		SubstantiouslyIll = 4, //Orange
		HeavilyIll = 5,
		Critical = 7, //flashing
		Defibrillatorble = 7, //that defibrillator one
		NoSoul = 8, //Skull, you are Dead! no, you! Pow hAhA. You are dead, no big surprise
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
