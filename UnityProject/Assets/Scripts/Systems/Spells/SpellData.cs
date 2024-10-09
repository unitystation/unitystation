using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NaughtyAttributes;
using AddressableReferences;
using Logs;
using Systems.Spells;
using Tiles;

namespace ScriptableObjects.Systems.Spells
{
	/// <summary>
	/// All data related to a spell
	/// </summary>
	[CreateAssetMenu(fileName = "MySpell", menuName = "ScriptableObjects/Systems/Spells/SpellData")]
	public class SpellData : ActionData, ICooldown
	{
		public short Index => (short)SpellList.Instance.Spells.IndexOf(this);

		//ignoring ticks from SO,
		//we use our own command-based invocation for spells
		public override bool CallOnClient => true;
		public override bool CallOnServer => false;

		public bool ShouldDespawn => SummonLifespan > 0f;

		[Tooltip("Implementation prefab, defaults to SimpleSpell if null")]
		[SerializeField]
		private GameObject spellImplementation = null;


		[Tooltip("Rechargeable has unlimited uses, LimitedCharges is limited by StartingCharges")]
		[SerializeField, BoxGroup("Replenishment")]
		private SpellChargeType chargeType = SpellChargeType.Rechargeable;
		[Tooltip("Cooldown time in seconds.")]
		[SerializeField, BoxGroup("Replenishment"), ShowIf(nameof(IsRechargeable)), Range(0f, 1000f)]
		private float cooldownTime = 10f;
		[Tooltip("Starting charges. Used if ChargeType = FixedCharges")]
		[SerializeField, BoxGroup("Replenishment"), HideIf(nameof(IsRechargeable)), Range(0, 30)]
		private int startingCharges = 10;

		[SerializeField] private AddressableAudioSource castSound = null;
		[SerializeField] private int range = 0;

		[SerializeField, BoxGroup("Chat")]
		private string stillRechargingMessage = "The spell is still recharging!";
		[Tooltip("Whatever it says to the guy affected by it")]
		[SerializeField, BoxGroup("Chat")]
		private string affectedMessage = "";
		[Tooltip("Whether to whisper, shout or emote the invocationMessage")]
		[SerializeField, BoxGroup("Chat")]
		private SpellInvocationType invocationType = SpellInvocationType.None;
		[SerializeField, BoxGroup("Chat"), ShowIf(nameof(IsInvocable))]
		private string invocationMessage = "";
		[SerializeField, BoxGroup("Chat"), ShowIf(nameof(IsInvocable))]
		private string invocationMessageSelf = "";

		[Tooltip("Summon type (what to spawn)")]
		[SerializeField, BoxGroup("Summoning")]
		private SpellSummonType summonType = SpellSummonType.None;
		[Tooltip("Objects to summon (SummonType=Object)")]
		[SerializeField, BoxGroup("Summoning"), ShowIf(nameof(WillSummonObjects))]
		private List<GameObject> summonObjects = new List<GameObject>();
		[Tooltip("Tiles to summon (SummonType=Tile)")]
		[SerializeField, BoxGroup("Summoning"), ShowIf(nameof(WillSummonTiles))]
		private List<LayerTile> summonTiles = new List<LayerTile>();
		[Tooltip("Summon position type")]
		[SerializeField, BoxGroup("Summoning"), ShowIf(nameof(WillSummonThing))]
		private SpellSummonPosition summonPosition = SpellSummonPosition.CasterDirection;
		[Tooltip("0 means permanent – lifespan of summoned thing in seconds")]
		[SerializeField, BoxGroup("Summoning"), Range(0f, 1000f), ShowIf(nameof(WillSummonThing))]
		private float summonLifespan = 10f;
		[Tooltip("Whether to replace existing tile")]
		[SerializeField, BoxGroup("Summoning"), ShowIf(nameof(WillSummonThing))]
		private bool replaceExisting = false;

		public string StillRechargingMessage => stillRechargingMessage;
		public SpellChargeType ChargeType => chargeType;
		public float CooldownTime => cooldownTime;
		public int StartingCharges => startingCharges;
		public string AffectedMessage => affectedMessage;
		public AddressableAudioSource CastSound => castSound;
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
		public GameObject SpellImplementation => spellImplementation;

		public float DefaultTime => CooldownTime;

		#region For inspector

		private bool IsRechargeable => ChargeType == SpellChargeType.Rechargeable;
		private bool IsInvocable => InvocationType != SpellInvocationType.None;
		private bool WillSummonObjects => SummonType == SpellSummonType.Object;
		private bool WillSummonTiles => SummonType == SpellSummonType.Tile;
		private bool WillSummonThing => SummonType != SpellSummonType.None;

		#endregion

		[NaughtyAttributes.Button()]
		public void CheckImplementation()
		{
			if (spellImplementation == null && SpellList.Instance != null)
			{
				spellImplementation = SpellList.Instance.DefaultImplementation;
#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
#endif
			}
		}

		public Spell AddToPlayer(Mind player)
		{
			var spellObject = Instantiate(SpellImplementation, player.gameObject.transform);
			var spellComponent = spellObject.GetComponent<Spell>();
			if (spellComponent == null)
			{
				Loggy.LogError($"No spell component found on {spellObject} for {this}!", Category.Spells);
				return default;
			}
			spellComponent.SpellData = this;
			spellComponent.CooldownTime = CooldownTime;
			return spellComponent;
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
		None, Object, Tile
	}

	public enum SpellSummonPosition
	{
		CasterTile, CasterDirection, Custom
	}
}
