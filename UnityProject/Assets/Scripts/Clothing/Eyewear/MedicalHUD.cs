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
		var newIcon = HealthSymbol.Healthy;
		var barPercentage = HealthBarPercentage.Dead;
		var healthPercentage = 0f;
		if (HealthStateController.MaxHealth != 0) healthPercentage = newHealth / HealthStateController.MaxHealth;

		int currentStage = GetCurrentSicknessStage();
		switch (currentStage)
		{
			case 0:
				newIcon = HealthSymbol.Healthy;
				break;
			case 1:
				newIcon = HealthSymbol.SlightlyIll;
				break;
			case 2:
				newIcon = HealthSymbol.ModeratelyIll;
				break;
			case 3:
				newIcon = HealthSymbol.SubstantiouslyIll;
				break;
			case >=4:
				newIcon = HealthSymbol.HeavilyIll;
				break;
		}

		if (HealthStateController.ConsciousState == ConsciousState.DEAD)
			newIcon = PlayerScript.HasSoul ? HealthSymbol.Defibrillatorble : HealthSymbol.NoSoul;
		else GetHealthBarPercentage(healthPercentage, ref barPercentage, ref newIcon);

		SyncHealthBarPercentage(CurrentHealthBarPercentage, barPercentage);
		SyncCurrentState(CurrentState, newIcon);
	}

	private void GetHealthBarPercentage(float healthPercentage, ref HealthBarPercentage barPercentage, ref HealthSymbol iconOverride)
	{
		switch (healthPercentage)
		{
			case > 1.25f:
				barPercentage = HealthBarPercentage.Full100;
				iconOverride = HealthSymbol.Buffed;
				break;
			case >= 1.0f:
				barPercentage = HealthBarPercentage.Full100;
				break;
			case >= 0.93f:
				barPercentage = HealthBarPercentage.Damaged93;
				break;
			case >= 0.87f:
				barPercentage = HealthBarPercentage.Damaged87;
				break;
			case >= 0.81f:
				barPercentage = HealthBarPercentage.Damaged81;
				break;
			case >= 0.75f:
				barPercentage = HealthBarPercentage.Damaged75;
				break;
			case >= 0.68f:
				barPercentage = HealthBarPercentage.Damaged68;
				break;
			case >= 0.62f:
				barPercentage = HealthBarPercentage.Damaged62;
				break;
			case >= 0.56f:
				barPercentage = HealthBarPercentage.Damaged56;
				break;
			case >= 0.50f:
				barPercentage = HealthBarPercentage.Damaged50;
				break;
			case >= 0.43f:
				barPercentage = HealthBarPercentage.Damaged43;
				break;
			case >= 0.37f:
				barPercentage = HealthBarPercentage.Damaged37;
				break;
			case >= 0.31f:
				barPercentage = HealthBarPercentage.Damaged31;
				break;
			case >= 0.25f:
				barPercentage = HealthBarPercentage.Damaged25;
				break;
			case >= 0.18f:
				barPercentage = HealthBarPercentage.Damaged18;
				break;
			case >= 0.125f:
				barPercentage = HealthBarPercentage.Damaged12_5;
				break;
			case >= 0.065f:
				barPercentage = HealthBarPercentage.Damaged6_5;
				break;
			case >= 0f:
				barPercentage = HealthBarPercentage.Damaged0;
				break;
			case >= -0.5f:
				barPercentage = HealthBarPercentage.CriticalN50;
				iconOverride = HealthSymbol.Critical;
				break;
			default:
				barPercentage = HealthBarPercentage.CriticalN85;
				iconOverride = HealthSymbol.Critical;
				break;
		}
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

			float concentrationPercent = (amount / system.NormalBlood) * 100;
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
