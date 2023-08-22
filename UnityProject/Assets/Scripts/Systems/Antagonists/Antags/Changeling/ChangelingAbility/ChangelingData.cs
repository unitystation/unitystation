using AddressableReferences;
using Chemistry;
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
		[Tooltip("Description that will be used in store")]
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
		public int AbilityEPCost => abilityEPCost;
		[Tooltip("Chemical points cost for use")]
		[SerializeField] private int abilityChemCost;
		public int AbilityChemCost => abilityChemCost;

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
		[SerializeField] private bool isToggleable = false;
		public bool IsToggleable => isToggleable;

		[Tooltip("Is ability will be showed in abilites store")]
		[SerializeField] private bool showInStore = true;
		public bool ShowInStore => showInStore;

		[ShowIf("ShowIfToggle")]
		[SerializeField] private bool canBeUsedWhileInCrit = false;
		public bool CanBeUsedWhileInCrit => canBeUsedWhileInCrit;

		[Tooltip("Shows only when absorbed someone")]
		[SerializeField] private bool showsOnlyWhenAbsorbedSomeone = false;
		public bool ShowsOnlyWhenAbsorbedSomeone => showsOnlyWhenAbsorbedSomeone;

		[ShowIf("ShowIfToggle")]
		[SerializeField] private bool swithedToOnWhenInCrit = false;
		public bool SwithedToOnWhenInCrit => swithedToOnWhenInCrit;

		[ShowIf("ShowIfToggle")]
		[SerializeField] private bool swithedToOffWhenExitCrit = false;
		public bool SwithedToOffWhenExitCrit => swithedToOffWhenExitCrit;

		[Tooltip("Activats cooldown when ability is toggled anytime. Not after ability is toggled off only")]
		[ShowIf("ShowIfToggle")]
		[SerializeField] private bool cooldownWhenToggled = false;
		public bool CooldownWhenToggled => cooldownWhenToggled;

		[ShowIf("ShowIfToggle")]
		[SerializeField] private bool drawCostWhenToggledOn = false;
		public bool DrawCostWhenToggledOn => drawCostWhenToggledOn;

		[ShowIf("ShowIfToggle")]
		[SerializeField] private bool drawCostWhenToggledOff = false;
		public bool DrawCostWhenToggledOff => drawCostWhenToggledOff;

		[SerializeField] private bool showInActions = true;
		public bool ShowInActions => showInActions;

		[SerializeField] private int cooldown = 1;
		public float DefaultTime => cooldown;

		[ShowIf("ShowIfEyeModifyer")]
		[SerializeField] private Vector3 expandedNightVisionVisibility = new (25, 25, 42);
		public Vector3 ExpandedNightVisionVisibility => expandedNightVisionVisibility;

		[ShowIf("ShowIfEyeModifyer")]
		[SerializeField] private float defaultvisibilityAnimationSpeed = 1.25f;
		public float DefaultvisibilityAnimationSpeed => defaultvisibilityAnimationSpeed;

		[ShowIf("ShowIfEyeModifyer")]
		[SerializeField] private float revertvisibilityAnimationSpeed = 0.2f;
		public float RevertvisibilityAnimationSpeed => revertvisibilityAnimationSpeed;

		[ShowIf("ShowIfSting")]
		[SerializeField] private float stingTime = 4f;
		public float StingTime => stingTime;

		[ShowIf("ShowIfNeedReagent")]
		[SerializeField] private Reagent reagent;
		public Reagent Reagent => reagent;

		[ShowIf("ShowIfNeedReagent")]
		[SerializeField] private float reagentCount = 25;
		public float ReagentCount => reagentCount;

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
		private bool ShowIfToggle()
		{
			return IsToggleable;
		}

		private bool ShowIfSting()
		{
			return abilityType == ChangelingAbilityType.Sting;
		}

		private bool ShowIfEyeModifyer()
		{
			return abilityType == ChangelingAbilityType.Misc && miscType == ChangelingMiscType.AugmentedEyesight;
		}

		private bool ShowIfNeedReagent()
		{
			return abilityType == ChangelingAbilityType.Sting && stingType == StingType.HallucinationSting;
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

		public virtual bool PerfomAbilityClient(ChangelingAbility abil)
		{
			UIManager.Display.hudChangeling.ChangelingMain.UseAbility(abil);
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

		public ChangelingAbility AddToPlayer(Mind player)
		{
			var abilityObject = Instantiate(AbilityImplementation, player.gameObject.transform);
			var abilityComponent = abilityObject.GetComponent<ChangelingAbility>();
			if (abilityComponent == null)
			{
				Logger.LogError($"No ability component found on {abilityObject} for {this}!", Category.Changeling);
				return default;
			}
			abilityComponent.ability = this;
			abilityComponent.CooldownTime = cooldown;
			return abilityComponent;
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
		Transform
	}

	public enum ChangelingMiscType
	{
		AugmentedEyesight,
		OpenStore,
		OpenMemories,
		OpenTransform
	}
}