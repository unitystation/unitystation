using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// All data related to a spell
/// </summary>
[CreateAssetMenu(fileName = "MySpell", menuName = "ScriptableObjects/SpellData")]
public class SpellData : ActionData, ICooldown
{
	//don't touch, assigned automatically in runtime
	[NonSerialized] public int index = -1;

	//ignoring ticks from SO,
	//we need both calls to always be executed for spells
	public override bool CallOnClient => true;
	public override bool CallOnServer => true;

	public bool ShouldDespawn => SummonLifespan > 0f;

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

	[Header("Whether to replace existing tile")]
	[SerializeField] private bool replaceExisting = false;

	public string Name => spellName;
	public string Description => description;
	public string StillRechargingMessage => stillRechargingMessage;
	public SpellChargeType ChargeType => chargeType;
	public float CooldownTime => cooldownTime;
	public int StartingCharges => startingCharges;
	public string AffectedMessage => affectedMessage;
	public string CastSound => castSound;
	public SpellInvocationType InvocationType => invocationType;
	public string InvocationMessage => invocationMessage;
	public int Range => range;
	public string InvocationMessageSelf => invocationMessageSelf;
	public float SummonLifespan => summonLifespan;
	public SpellSummonType SummonType => summonType;
	public List<GameObject> SummonObjects => summonObjects;
	public List<LayerTile> SummonTiles => summonTiles;
	public SpellSummonPosition SummonPosition => summonPosition;
	public bool ReplaceExisting => replaceExisting;

	public float DefaultTime => CooldownTime;
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