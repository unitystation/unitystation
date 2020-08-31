using AdminTools;
using Health.Sickness;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Provides central access to the Players Health
/// </summary>
public class PlayerHealth : LivingHealthBehaviour
{
	[SerializeField]
	private MetabolismSystem metabolism = null;

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

	public MetabolismSystem Metabolism { get => metabolism; }

	/// <summary>
	/// Current sicknesses status of the player and their current stage.
	/// </summary>
	private PlayerSickness playerSickness = null;

	/// <summary>
	/// List of sicknesses that player has gained immunity.
	/// </summary>
	private List<Sickness> immunedSickness;

	public PlayerMove PlayerMove;
	private PlayerSprites playerSprites;

	private PlayerNetworkActions playerNetworkActions;
	/// <summary>
	/// Cached register player
	/// </summary>
	private RegisterPlayer registerPlayer;

	private ItemStorage itemStorage;

	private bool init = false;

	//fixme: not actually set or modified. keep an eye on this!
	public bool serverPlayerConscious { get; set; } = true; //Only used on the server

	public override void Awake()
	{
		base.Awake();
		EnsureInit();
		ApplyStartingAllergies();
	}

	// At round start, a percent of players start with mild allergy
	// The purpose of this, is to make believe that coughing and sneezing at random is "probably" not a real sickness.
	private void ApplyStartingAllergies()
	{
		if (UnityEngine.Random.Range(0, 100) < percentAllergies)
		{
			AddSickness(commonAllergies);
		}
	}

	/// <summary>
	/// Add a sickness to the player if he doesn't already has it and isn't immuned
	/// </summary>
	/// <param name="">The sickness to add</param>
	public void AddSickness(Sickness sickness)
	{
		if (IsDead)
			return;

		if ((!playerSickness.HasSickness(sickness)) && (!immunedSickness.Contains(sickness)))
			playerSickness.Add(sickness, Time.time);
	}

	/// <summary>
	/// This will remove the sickness from the player, healing him.
	/// </summary>
	/// <remarks>Thread safe</remarks>
	public void RemoveSickness(Sickness sickness)
	{
		SicknessAffliction sicknessAffliction = playerSickness.sicknessAfflictions.FirstOrDefault(p => p.Sickness == sickness);

		if (sicknessAffliction)
			sicknessAffliction.Heal();
	}

	/// <summary>
	/// This will remove the sickness from the player, healing him.  This will also make him immune for the current round.
	/// </summary>
	public void ImmuneSickness(Sickness sickness)
	{
		RemoveSickness(sickness);

		if (!immunedSickness.Contains(sickness))
			immunedSickness.Add(sickness);
	}


	void EnsureInit()
	{
		if (init) return;

		init = true;
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		PlayerMove = GetComponent<PlayerMove>();
		playerSprites = GetComponent<PlayerSprites>();
		registerPlayer = GetComponent<RegisterPlayer>();
		itemStorage = GetComponent<ItemStorage>();
		playerSickness = GetComponent<PlayerSickness>();

		OnConsciousStateChangeServer.AddListener(OnPlayerConsciousStateChangeServer);

		immunedSickness = new List<Sickness>();

		metabolism = GetComponent<MetabolismSystem>();
		if (metabolism == null)
		{
			metabolism = gameObject.AddComponent<MetabolismSystem>();
		}
	}

	public override void OnStartClient()
	{
		EnsureInit();
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		EnsureInit();
		base.OnStartServer();
	}

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
				killerName = "Stressful work";
			}

			string playerName = player?.Name ?? "dummy";
			if (killerName == playerName)
			{
				Chat.AddActionMsgToChat(gameObject, "You committed suicide, what a waste.", $"{playerName} committed suicide.");
			}
			else if (killerName.EndsWith(playerName))
			{
				// chain reactions
				Chat.AddActionMsgToChat(gameObject, $" You screwed yourself up with some help from {killerName}",
					$"{playerName} screwed himself up with some help from {killerName}");
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
				EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
				string descriptor = null;
				if (player != null)
				{
					descriptor = player.CharacterSettings?.TheirPronoun();
				}

				if (descriptor == null)
				{
					descriptor = "their";
				}

				Chat.AddLocalMsgToChat($"<b>{playerName}</b> seizes up and falls limp, {descriptor} eyes dead and lifeless...", (Vector3)registerPlayer.WorldPositionServer, gameObject);
			}

			PlayerDeathMessage.Send(gameObject);
		}
	}

	[ClientRpc]
	private void RpcPassBullets(GameObject target)
	{
		foreach (BoxCollider2D comp in target.GetComponents<BoxCollider2D>())
		{
			if (!comp.isTrigger)
			{
				comp.enabled = false;
			}
		}
	}

	[Server]
	public void ServerGibPlayer()
	{
		Gib();
	}

	protected override void Gib()
	{
		Death();
		EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
		//drop clothes, gib... but don't destroy actual player, a piece should remain

		//drop everything
		foreach (var slot in itemStorage.GetItemSlots())
		{
			Inventory.ServerDrop(slot);
		}

		PlayerMove.PlayerScript.pushPull.VisibleState = false;
		playerNetworkActions.ServerSpawnPlayerGhost();
	}

	///     make player unconscious upon crit
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

	/// These electrocution methods are specific to players,
	/// and they assume the player mob is humanoid.
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
		ApplyDamageToBodypart(null, damage, AttackType.Internal, DamageType.Burn, bodypart);
	}

	protected override void MildElectrocution(Electrocution electrocution, float shockPower)
	{
		SoundManager.PlayNetworkedAtPos("SmallElectricShock#", registerPlayer.WorldPosition);
		Chat.AddExamineMsgFromServer(gameObject, $"The {electrocution.ShockSourceName} gives you a slight tingling sensation...");
	}

	protected override void PainfulElectrocution(Electrocution electrocution, float shockPower)
	{
		// TODO: Add sparks VFX at shockSourcePos.
		SoundManager.PlayNetworkedAtPos("Sparks#", electrocution.ShockSourcePos);
		Inventory.ServerDrop(itemStorage.GetActiveHandSlot());
		// Slip is essentially a yelp SFX.
		SoundManager.PlayNetworkedAtPos("Slip", registerPlayer.WorldPosition,
				UnityEngine.Random.Range(0.4f, 1.2f), sourceObj: gameObject);

		string victimChatString = (electrocution.ShockSourceName != null ? $"The {electrocution.ShockSourceName}" : "Something") +
				" gives you a small electric shock!";
		Chat.AddExamineMsgFromServer(gameObject, victimChatString);

		DealElectrocutionDamage(5, electrocutedHand);
	}

	protected override void LethalElectrocution(Electrocution electrocution, float shockPower)
	{

		PlayerMove.allowInput = false;
		// TODO: Add sparks VFX at shockSourcePos.
		SoundManager.PlayNetworkedAtPos("Sparks#", electrocution.ShockSourcePos);
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
		SoundManager.PlayNetworkedAtPos("Bodyfall", registerPlayer.WorldPosition,
				UnityEngine.Random.Range(0.8f, 1.2f), sourceObj: gameObject);

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
