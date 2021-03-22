﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdminTools;
using Health.Sickness;
using Messages.Server;
using Messages.Server.SoundMessages;
using Mirror;
using UnityEngine;

namespace HealthV2
{
	public class PlayerHealthV2 : LivingHealthMasterBase, RegisterPlayer.IControlPlayerState
	{

		private PlayerMove playerMove;
		/// <summary>
		/// Controller for sprite direction and walking into objects
		/// </summary>
		public PlayerMove PlayerMove => playerMove;

		private PlayerSprites playerSprites;


		private PlayerScript playerScript;
		/// <summary>
		/// The associated Player Script
		/// </summary>
		public PlayerScript PlayerScript => playerScript;

		private PlayerNetworkActions playerNetworkActions;

		private RegisterPlayer registerPlayer;
		/// <summary>
		/// Cached register player
		/// </summary>
		public RegisterPlayer RegisterPlayer => registerPlayer;


		private Equipment equipment;
		/// <summary>
		/// The associated Player Equipment
		/// </summary>
		public Equipment Equipment => equipment;

		private ItemStorage itemStorage;

		private bool init = false;

		/// <summary>
		/// The percentage of players that start with common allergies.
		/// </summary>
		[SerializeField]
		private int percentAllergies = 30;

		/// <summary>
		/// Common allergies.  A percent of players start with that.
		/// </summary>
		[SerializeField]
		private Sickness commonAllergies = null;

		//fixme: not actually set or modified. keep an eye on this!
		public bool serverPlayerConscious { get; set; } = true; //Only used on the server

		public override void EnsureInit()
		{
			if (init) return;
			init = true;
			base.EnsureInit();
			playerNetworkActions = GetComponent<PlayerNetworkActions>();
			playerMove = GetComponent<PlayerMove>();
			playerSprites = GetComponent<PlayerSprites>();
			registerPlayer = GetComponent<RegisterPlayer>();
			itemStorage = GetComponent<ItemStorage>();
			equipment = GetComponent<Equipment>();
			playerScript = GetComponent<PlayerScript>();
			OnConsciousStateChangeServer.AddListener(OnPlayerConsciousStateChangeServer);
			registerPlayer.AddStatus(this);
		}

		private void OnPlayerConsciousStateChangeServer(ConsciousState oldState, ConsciousState newState)
		{
			if (playerNetworkActions == null || registerPlayer == null) EnsureInit();

			if (isServer)
			{
				playerNetworkActions.OnConsciousStateChanged(oldState, newState);
			}

			//we stay upright if buckled or conscious
			registerPlayer.ServerSetIsStanding(newState == ConsciousState.CONSCIOUS || PlayerMove.IsBuckled);
		}

		/// <summary>
		/// Server only. Gibs the player.
		/// </summary>
		[Server]
		public void ServerGibPlayer()
		{
			Gib();
		}

		protected override void Gib()
		{
			Death();
			EffectsFactory.BloodSplat(RegisterTile.WorldPositionServer, BloodSplatSize.large, BloodSplatType.red);
			//drop clothes, gib... but don't destroy actual player, a piece should remain

			//drop everything
			foreach (var slot in itemStorage.GetItemSlots())
			{
				Inventory.ServerDrop(slot);
			}

			PlayerMove.PlayerScript.pushPull.VisibleState = false;
			playerNetworkActions.ServerSpawnPlayerGhost();
		}

		bool RegisterPlayer.IControlPlayerState.AllowChange(bool rest)
		{
			if (rest)
			{
				return true;
			}
			else
			{
				return ConsciousState == ConsciousState.CONSCIOUS;
			}

		}

		/// <summary>
		/// Actions the server performs when the player dies
		/// </summary>
		protected override void OnDeathActions()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				ConnectedPlayer player = PlayerList.Instance.Get(gameObject);

				string killerName = null;
				if (LastDamagedBy != null)
				{
					var lastDamager = PlayerList.Instance.Get(LastDamagedBy);
					if (lastDamager != null)
					{
						killerName = lastDamager.Name;
						AutoMod.ProcessPlayerKill(lastDamager, player);
					}
				}

				if (killerName == null)
				{
					killerName = "stressful work";
				}

				string playerName = player?.Name ?? "dummy";
				if (killerName == playerName)
				{
					Chat.AddActionMsgToChat(gameObject, "You committed suicide, what a waste.", $"{playerName} committed suicide.");
				}
				else if (killerName.EndsWith(playerName))
				{
					string themself = null;
					if (player != null)
					{
						themself = player.CharacterSettings?.ThemselfPronoun(player.Script);
					}
					if (themself == null)
					{
						themself = "themself";
					}
					//chain reactions
					Chat.AddActionMsgToChat(gameObject, $"You screwed yourself up with some help from {killerName}",
						$"{playerName} screwed {themself} up with some help from {killerName}");
				}
				else
				{
					PlayerList.Instance.TrackKill(LastDamagedBy, gameObject);
				}

				//drop items in hand
				if (itemStorage != null)
				{
					Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.leftHand));
					Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.rightHand));
				}

				if (isServer)
				{
					EffectsFactory.BloodSplat(RegisterTile.WorldPositionServer, BloodSplatSize.large, BloodSplatType.red);
					string their = null;
					if (player != null)
					{
						their = player.CharacterSettings?.TheirPronoun(player.Script);
					}

					if (their == null)
					{
						their = "their";
					}

					Chat.AddLocalMsgToChat($"<b>{player.Name}</b> seizes up and falls limp, {their} eyes dead and lifeless...", gameObject);
				}

				TriggerEventMessage.SendTo(gameObject, EVENT.PlayerDied);
			}
		}

		#region Sickness
		//Player only sickness stuff, general stuff in LivingHealthMasterBase as all mobs should be able to get sick

		/// <summary>
		/// Randomly determines whether the player has common allergies at round start
		/// This is to give the idea that coughing and sneezing at random is "probably" not a real sickness.
		/// </summary>
		private void ApplyStartingAllergies()
		{
			if (UnityEngine.Random.Range(0, 100) < percentAllergies)
			{
				AddSickness(commonAllergies);
			}
		}

		#endregion

		#region Electrocution

		private const int ELECTROCUTION_BURNDAMAGE_MODIFIER = 100; // Less is more.
		private const int ELECTROCUTION_MAX_DAMAGE = 100; // -1 to disable limit
		private const int ELECTROCUTION_STUN_PERIOD = 10; // In seconds.
		private const int ELECTROCUTION_ANIM_PERIOD = 5; // Set less than stun period.
		private const int ELECTROCUTION_MICROLERP_PERIOD = 15;
		private BodyPartType electrocutedHand;

		/// <summary>
		/// Electrocutes a player, applying effects to the victim depending on the electrocution power.
		/// </summary>
		/// <param name="electrocution">The object containing all information for this electrocution</param>
		/// <returns>Returns an ElectrocutionSeverity for when the following logic depends on the elctrocution severity.</returns>
		public override LivingShockResponse Electrocute(Electrocution electrocution)
		{
			if (playerNetworkActions.activeHand == NamedSlot.leftHand)
			{
				electrocutedHand = BodyPartType.LeftArm;
			}
			else
			{
				electrocutedHand = BodyPartType.RightArm;
			}

			return base.Electrocute(electrocution);
		}

		/// <summary>
		/// Calculates the humanoid body's hand-to-foot electrical resistance based on the voltage.
		/// Based on the figures provided by Wikipedia's electrical injury page (hand-to-hand).
		/// Trends to 1200 Ohms at significant voltages.
		/// </summary>
		/// <param name="voltage">The potential difference across the human</param>
		/// <returns>float resistance</returns>
		private float GetNakedHumanoidElectricalResistance(float voltage)
		{
			float resistance = 1000 + (3000 / (1 + (float)Math.Pow(voltage / 55, 1.5f)));
			return resistance *= 1.2f; // A bit more resistance due to slightly longer (hand-foot) path.
		}

		/// <summary>
		/// Calculates the player's total resistance using a base humanoid resistance value,
		/// their health and the items the performer is wearing or holding.
		/// Assumes the player is a humanoid.
		/// </summary>
		/// <param name="voltage">The potential difference across the player</param>
		/// <returns>float resistance</returns>
		protected override float ApproximateElectricalResistance(float voltage)
		{
			// Assume the player is a humanoid
			float resistance = GetNakedHumanoidElectricalResistance(voltage);

			// Give the humanoid extra/less electrical resistance based on what they're holding/wearing
			resistance += Electrocution.GetItemElectricalResistance(itemStorage.GetNamedItemSlot(NamedSlot.hands).ItemObject);
			resistance += Electrocution.GetItemElectricalResistance(itemStorage.GetNamedItemSlot(NamedSlot.feet).ItemObject);
			// A solid grip on a conductive item will reduce resistance - assuming it is conductive.
			if (itemStorage.GetActiveHandSlot().Item != null) resistance -= 300;

			// Broken skin reduces electrical resistance - arbitrarily chosen at 4 to 1.
			resistance -= 4 * GetTotalBruteDamage();

			// Make sure the humanoid doesn't get ridiculous conductivity.
			if (resistance < 100) resistance = 100;
			return resistance;
		}

		/// <summary>
		/// Applies burn damage to the specified victim's bodyparts.
		/// Attack type is internal, so as to avoid needing to add electrical resistances to Armor class.
		/// </summary>
		/// <param name="damage">The amount of damage to apply to the bodypart</param>
		/// <param name="bodypart">The BodyPartType to damage.</param>
		private void DealElectrocutionDamage(float damage, BodyPartType bodypart)
		{
			ApplyDamageToBodyPart(null, damage, AttackType.Internal, DamageType.Burn, bodypart);
		}

		protected override void MildElectrocution(Electrocution electrocution, float shockPower)
		{
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.ElectricShock, registerPlayer.WorldPosition);
			Chat.AddExamineMsgFromServer(gameObject, $"The {electrocution.ShockSourceName} gives you a slight tingling sensation...");
		}

		protected override void PainfulElectrocution(Electrocution electrocution, float shockPower)
		{
			// TODO: Add sparks VFX at shockSourcePos.
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Sparks, electrocution.ShockSourcePos);
			Inventory.ServerDrop(itemStorage.GetActiveHandSlot());

			// Slip is essentially a yelp SFX.
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.4f, 1.2f));
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Slip, registerPlayer.WorldPosition,
					audioSourceParameters, sourceObj: gameObject);

			string victimChatString = (electrocution.ShockSourceName != null ? $"The {electrocution.ShockSourceName}" : "Something") +
					" gives you a small electric shock!";
			Chat.AddExamineMsgFromServer(gameObject, victimChatString);

			DealElectrocutionDamage(5, electrocutedHand);
		}

		protected override void LethalElectrocution(Electrocution electrocution, float shockPower)
		{

			PlayerMove.allowInput = false;
			// TODO: Add sparks VFX at shockSourcePos.
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Sparks, electrocution.ShockSourcePos);
			StartCoroutine(ElectrocutionSequence());

			string victimChatString, observerChatString;
			if (electrocution.ShockSourceName != null)
			{
				victimChatString = $"The {electrocution.ShockSourceName} electrocutes you!";
				observerChatString = $"{gameObject.ExpensiveName()} is electrocuted by the {electrocution.ShockSourceName}!";
			}
			else
			{
				victimChatString = $"Something electrocutes you!";
				observerChatString = $"{gameObject.ExpensiveName()} is electrocuted by something!";
			}
			Chat.AddCombatMsgToChat(gameObject, victimChatString, observerChatString);

			var damage = shockPower / ELECTROCUTION_BURNDAMAGE_MODIFIER;
			if (ELECTROCUTION_MAX_DAMAGE != -1 && damage > ELECTROCUTION_MAX_DAMAGE) damage = ELECTROCUTION_MAX_DAMAGE;
			DealElectrocutionDamage(damage * 0.4f, electrocutedHand);
			DealElectrocutionDamage(damage * 0.25f, BodyPartType.Chest);
			DealElectrocutionDamage(damage * 0.175f, BodyPartType.LeftLeg);
			DealElectrocutionDamage(damage * 0.175f, BodyPartType.RightLeg);
		}

		private IEnumerator ElectrocutionSequence()
		{
			float timeBeforeDrop = 0.5f;

			RpcToggleElectrocutedOverlay();
			// TODO: Add micro-lerping here. (Player quick but short vertical, horizontal movements)

			yield return WaitFor.Seconds(timeBeforeDrop); // Instantly dropping to ground looks odd.
														  // TODO: Add sparks VFX at shockSourcePos.
			registerPlayer.ServerStun(ELECTROCUTION_STUN_PERIOD - timeBeforeDrop);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Bodyfall, registerPlayer.WorldPosition,
					audioSourceParameters, sourceObj: gameObject);

			yield return WaitFor.Seconds(ELECTROCUTION_ANIM_PERIOD - timeBeforeDrop);
			RpcToggleElectrocutedOverlay();

			//yield return WaitFor.Seconds(ELECTROCUTION_MICROLERP_PERIOD - ELECTROCUTION_ANIM_PERIOD - timeBeforeDrop);
			// TODO: End micro-lerping here.
		}

		[ClientRpc]
		private void RpcToggleElectrocutedOverlay()
		{
			playerSprites.ToggleElectrocutedOverlay();
		}

		#endregion Electrocution
	}
}