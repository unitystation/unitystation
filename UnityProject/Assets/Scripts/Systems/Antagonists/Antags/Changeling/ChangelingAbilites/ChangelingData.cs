using AddressableReferences;
using NaughtyAttributes;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Abilities/Ability")]
	public class ChangelingData : ActionData, ICooldown
	{
		public short Index => (short)ChangelingAbilityList.Instance.Abilites.IndexOf(this);

		public override bool CallOnClient => true;
		public override bool CallOnServer => false;

		[TextArea(8, 20)]
		[SerializeField] protected string descriptionStore = "";
		public string DescriptionStore
		{
			get
			{
				if (descriptionStore == null || descriptionStore.Length == 0)
					return description;
				return descriptionStore;
			}
		}

		[Header("Variables")]
		// ep - evolution point
		[Tooltip("Evolution points cost for buying")]
		[SerializeField] private int abilityEPCost;
		[SerializeField] public int AbilityEPCost => abilityEPCost;
		[Tooltip("Chemical points cost for use")]
		[SerializeField] private int abilityChemCost;
		[SerializeField] public int AbilityChemCost => abilityChemCost;

		[Tooltip("Sount called when ability is used")]
		[SerializeField] private AddressableAudioSource castSound = null;
		public AddressableAudioSource CastSound => castSound;


		[Tooltip("Is action can be reseted")]
		public bool canBeReseted = false;
		[Tooltip("Is ability will be added on start")]
		public bool startAbility = false;
		[Tooltip("Is ability slows chem generation while active")]
		[SerializeField] bool isSlowingChemRegeneration = false;
		public bool IsSlowingChemRegeneration => isSlowingChemRegeneration;
		[Tooltip("Is ability stops chem generation while active")]
		[SerializeField] bool isStopingChemRegeneration = false;
		public bool IsStopingChemRegeneration => isStopingChemRegeneration;
		[Tooltip("Is ability called on client")]
		[SerializeField] private bool isLocal = false;
		public bool IsLocal => isLocal;
		[SerializeField] private GameObject dnaPrefab;
		public GameObject DnaPrefab => dnaPrefab;

		[SerializeField] private bool showInStore = true;
		public bool ShowInStore => showInStore;

		[SerializeField] private bool showInActions = true;
		public bool ShowInActions => showInActions;

		[SerializeField] private int cooldown = 1;
		public float DefaultTime => cooldown;

		public GameObject AbilityImplementation => abilityImplementation;


		[Tooltip("What ability type is it")]
		public ChangelingAbilityType abilityType = ChangelingAbilityType.Misc;

		[Tooltip("What sting type is it")]
		[ShowIf("ShowIfSting")]
		public StingType stingType;

		[Tooltip("What heal type is it")]
		[ShowIf("ShowIfHeal")]
		public ChangelingHealType healType;

		[Tooltip("What transform type is it")]
		[ShowIf("ShowIfTransform")]
		public ChangelingTransformType transformType;

		[ShowIf("ShowIfLocal")]
		[Tooltip("What ability use when choised")]
		[SerializeField] private ChangelingData useAfterChoise = null;
		public ChangelingData UseAfterChoise => useAfterChoise;

		[Tooltip("What transform type is it")]
		[ShowIf("ShowIfMisc")]
		public ChangelingMiscType miscType;

		[Tooltip("Implementation prefab, defaults to SimpleSpell if null")]
		[SerializeField] private GameObject abilityImplementation = null;

		#region Inspector

		#if UNITY_EDITOR
		private bool ShowIfSting()
		{
			return abilityType == ChangelingAbilityType.Sting;
		}

		private bool ShowIfHeal()
		{
			return abilityType == ChangelingAbilityType.Heal;
		}

		private bool ShowIfTransform()
		{
			return abilityType == ChangelingAbilityType.Transform;
		}

		private bool ShowIfMisc()
		{
			return abilityType == ChangelingAbilityType.Misc;
		}

		private bool ShowIfLocal()
		{
			return IsLocal;
		}

		public virtual bool PerfomAbility(ChangelingMain changeling, Vector3 objToPerfom)
		{
			return true;
		}

		public virtual bool PerfomAbilityClient()
		{
			return true;
		}
		#endif
		#endregion

		/// <summary>
		/// Returns ability price as tuple.
		/// </summary>
		/// <returns>Tuple where first value is evolution point cost and second is chemical cost</returns>
		public (int, int) GetAbilityPrice()
		{
			return (abilityEPCost, abilityChemCost);
		}

		protected bool AbilityValidation(ChangelingMain changeling)
		{
			return changeling.HasAbility(this);
		}

		public ChangelingAbility AddToPlayer(Mind player)
		{
			var spellObject = Instantiate(AbilityImplementation, player.gameObject.transform);
			var spellComponent = spellObject.GetComponent<ChangelingAbility>();
			if (spellComponent == null)
			{
				Logger.LogError($"No ability component found on {spellObject} for {this}!", Category.Changeling);
				return default;
			}
			spellComponent.ability = this;
			spellComponent.CooldownTime = cooldown;
			return spellComponent;
		}
	}

	public enum ChangelingAbilityType
	{
		Sting,
		Heal,
		Transform,
		Misc
	}

	public enum StingType
	{
		ExtractDNASting,
		HallucinationSting,
		Absorb
	}

	public enum ChangelingHealType
	{
		Regenerate,
		RevivingStasis
	}

	public enum ChangelingTransformType
	{
		TransformMenuOpen,
		Transform
	}

	public enum ChangelingMiscType
	{
		AugmentedEyesight,
		OpenStore
	}
}