using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// All data related to a spell
/// </summary>
[CreateAssetMenu(fileName = "MySpell", menuName = "ScriptableObjects/SpellData")]
public class SpellData : ActionData
{
	//don't touch, assigned automatically in runtime
	[NonSerialized] public int index = -1;

	//ignoring ticks from SO,
	//we need both calls to always be executed for spells
	public override bool CallOnClient => true;
	public override bool CallOnServer => true;

	[SerializeField] private string spellName = "";
	[SerializeField] private string description = "";
	[SerializeField] private string stillRechargingMessage = "The spell is still recharging";

	[Header("Rechargeable has unlimited uses, LimitedCharges is limited by StartingCharges")]
	[SerializeField] private SpellChargeType chargeType = SpellChargeType.Rechargeable;

	[Header("Cooldown time in seconds.")]
	[Range(0f,1000f)]
	[SerializeField] private float cooldownTime = 10f;

	[Header("Starting charges. Used if ChargeType = FixedCharges")]
	[Range(0,30)]
	[SerializeField] private int startingCharges = 10;

	[Header("Summon type (what to spawn)")]
	[SerializeField] private SpellSummonType summonType = SpellSummonType.None;

	[Header("Objects to summon (SummonType=Object)")]
	[SerializeField] private List<GameObject> summonObjects = new List<GameObject>();
	[Header("Tiles to summon (SummonType=Tile)")]
	[SerializeField] private List<LayerTile> summonTiles = new List<LayerTile>();

	[Header("Summon position type")]
	[SerializeField] private SpellSummonPosition summonPosition = SpellSummonPosition.CasterDirection;

	[Header("0 means permanent – lifespan of summoned thing in seconds")]
	[Range(0f,1000f)]
	[SerializeField] private float summonLifespan = 10f;

	[Header("Whatever it says to the guy affected by it")]
	[SerializeField] private string affectedMessage = "";
	[SerializeField] private string castSound = null;

	[Header("Whether to whisper, shout or emote the invocationMessage")]
	[SerializeField] private SpellInvocationType invocationType = SpellInvocationType.None;
	[SerializeField] private string invocationMessage = "";

	[SerializeField] private int range = 0;
	[SerializeField] private string invocationMessageSelf = "";

	public string Name
	{
		get => spellName;
		protected set => spellName = value;
	}

	public string Description
	{
		get => description;
		protected set => description = value;
	}

	public string StillRechargingMessage
	{
		get => stillRechargingMessage;
		protected set => stillRechargingMessage = value;
	}

	public SpellChargeType ChargeType
	{
		get => chargeType;
		protected set => chargeType = value;
	}

	public float CooldownTime
	{
		get => cooldownTime;
		protected set => cooldownTime = value;
	}

	public int StartingCharges
	{
		get => startingCharges;
		protected set => startingCharges = value;
	}

	public string AffectedMessage
	{
		get => affectedMessage;
		protected set => affectedMessage = value;
	}

	public string CastSound
	{
		get => castSound;
		protected set => castSound = value;
	}

	public SpellInvocationType InvocationType
	{
		get => invocationType;
		protected set => invocationType = value;
	}

	public string InvocationMessage
	{
		get => invocationMessage;
		protected set => invocationMessage = value;
	}

	public int Range
	{
		get => range;
		protected set => range = value;
	}

	public string InvocationMessageSelf
	{
		get => invocationMessageSelf;
		protected set => invocationMessageSelf = value;
	}

	public float SummonLifespan
	{
		get => summonLifespan;
		protected set => summonLifespan = value;
	}

	public SpellSummonType SummonType
	{
		get => summonType;
		protected set => summonType = value;
	}

	public List<GameObject> SummonObjects
	{
		get => summonObjects;
		protected set => summonObjects = value;
	}

	public List<LayerTile> SummonTiles
	{
		get => summonTiles;
		protected set => summonTiles = value;
	}

	public SpellSummonPosition SummonPosition
	{
		get => summonPosition;
		protected set => summonPosition = value;
	}
}
public enum SpellChargeType
{
	Rechargeable, FixedCharges
}

public enum SpellInvocationType
{
	None, Emote, Shout, Whisper
}

public enum SpellSummonType
{
	None, Object, Tile, Mob
}

public enum SpellSummonPosition
{
	CasterTile, CasterDirection, Custom
}