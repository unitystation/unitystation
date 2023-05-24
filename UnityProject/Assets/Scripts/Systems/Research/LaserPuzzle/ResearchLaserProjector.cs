using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects.Engineering;
using Objects.Machines;
using Shared.Systems.ObjectConnection;
using Systems.Electricity;
using Systems.Research.Objects;
using UnityEngine;
using Weapons.Projectiles;

public class ResearchLaserProjector : ResearchPointMachine, ICheckedInteractable<HandApply>
{
	//TODO Dropped item from plinth
	//TODO Network
	//TODO Go through and balance items
	//TODO map

	//TODO Destroy item When hit
	//TODO Sprite collector
	//TODO https://www.youtube.com/watch?v=DwGcKFMxrmI


	public LaserProjection LaserProjectionprefab;

	private LaserProjection LivingLine;

	public GameObject LaserProjectilePrefab;

	private Rotatable Rotatable;
	private RegisterTile RegisterTile;

	public List<ResearchCollector> Collectors = new List<ResearchCollector>();

	public List<ResearchData> CollectedData = new List<ResearchData>();

	private Emitter Emitter;

	private bool OnCoolDown = false;

	public void Awake()
	{
		Emitter = this.GetComponent<Emitter>();
		Rotatable = this.GetComponent<Rotatable>();
		RegisterTile = this.GetComponent<RegisterTile>();
	}

	[NaughtyAttributes.Button()]
	public void TriggerLaser()
	{


		if (researchServer == null)
		{
			Logger.LogError("Server Not Set");
			return;
		}


		if (LivingLine != null)
		{
			Destroy(LivingLine.gameObject);
		}
		gameObject.GetComponent<Collider2D>().enabled = false;
		LivingLine = Instantiate(LaserProjectionprefab, this.transform);
		LivingLine.Initialise(gameObject, Rotatable.WorldDirection, this);
		gameObject.GetComponent<Collider2D>().enabled = true;
	}

	[NaughtyAttributes.Button()]

	public void FireLaser()
	{
		CollectedData.Clear();
		var range = 30f;


		var  Projectile=  ProjectileManager.InstantiateAndShoot(LaserProjectilePrefab,
			Rotatable.WorldDirection, gameObject, null, BodyPartType.None, range);

		var Data = Projectile.GetComponent<ContainsResearchData>();
		Data.Initialise(null,this);
		StartCoroutine(WaitForLasers());
	}
	private IEnumerator WaitForLasers()
	{
		yield return WaitFor.Seconds(10f);


		if (researchServer == null) yield break;

		// Group the data by technology into a dictionary
		var groupedData = CollectedData.GroupBy(d => d.Technology)
			.ToDictionary(g => g.Key, g => g.ToList());

		int TotalResearched = 0;

		foreach (var Technology in groupedData)
		{
			var TotalResearch = 0f;
			foreach (var DataPiece in Technology.Value)
			{
				TotalResearch += DataPiece.ResearchPower;
			}

			if (TotalResearch >= Technology.Key.ResearchCosts && researchServer.Techweb.ResearchedTech.Contains( Technology.Key) == false)
			{
				Chat.AddActionMsgToChat(gameObject, $" Enough data has been collected to research {Technology.Key.DisplayName} ");
				researchServer.Techweb.UnlockTechnology(Technology.Key);
			}
			else
			{
				var intTotalResearch = Mathf.RoundToInt(TotalResearch);
				TotalResearched += intTotalResearch;
				researchServer.AddResearchPoints(this,  intTotalResearch);

			}
		}
		Chat.AddActionMsgToChat(gameObject, $" Enough data has been collected to generate {TotalResearched} Research points ");
		CollectedData.Clear();
	}


	public void RegisterDataFromCollector(ResearchData Data){
		CollectedData.Add(Data);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (researchServer == null)
		{
			Chat.AddActionMsgToChat(this.gameObject, " The research laser beeps and boops Stating it's missing a connection to a research server ");
			return;
		}

		if (OnCoolDown)
		{
			Chat.AddActionMsgToChat(this.gameObject, " The research laser beeps and boops Stating it's still recharging ");
			return;
		}

		if (Emitter.ValidSetup() == false)
		{
			Chat.AddActionMsgToChat(this.gameObject, " The research laser beeps and boops Stating Still needs to be constructed fully and Powered");
			return;

		}

		if (interaction.IsAltClick)
		{
			FireLaser();
		}
		else
		{
			Chat.AddActionMsgToChat(this.gameObject, " The research laser beeps and boops. Firing test Projection, Remember to connect collectors to research laser ");

			TriggerLaser();
		}
		StartCoroutine(WaitForRecharge());
	}

	private IEnumerator WaitForRecharge()
	{
		OnCoolDown = true;
		yield return WaitFor.Seconds(10f);
		OnCoolDown = false;
	}




}
