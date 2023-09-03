using Changeling;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

namespace Changeling
{
	public class ChangelingBaseAbility: ActionData, ICooldown
	{
		public short Index => (short)ChangelingAbilityList.Instance.Abilites.IndexOf(this);

		[SerializeField] protected int cooldown = 1;
		public float DefaultTime => cooldown;

		[Tooltip("Evolution points cost for buying")]
		[SerializeField] protected int abilityEPCost;
		public int AbilityEPCost => abilityEPCost;
		[Tooltip("Chemical points cost for use")]
		[SerializeField] protected int abilityChemCost;
		public int AbilityChemCost => abilityChemCost;

		[SerializeField] protected GameObject abilityImplementation = null;
		public GameObject AbilityImplementation => abilityImplementation;
		public override bool CallOnClient => true;
		public override bool CallOnServer => false;

		[TextArea(8, 20)]
		[Tooltip("Description that will be used in store")]
		[SerializeField] protected string descriptionStore = "";
		[SerializeField] private bool canBeUsedWhileInCrit = false;
		public bool CanBeUsedWhileInCrit => canBeUsedWhileInCrit;
		[SerializeField] private bool isLocal = false;
		public bool IsLocal => isLocal;
		public string DescriptionStore
		{
			get
			{
				if (descriptionStore == null || descriptionStore.Length == 0)
					return description;
				return descriptionStore;
			}
		}

		[Tooltip("Is the ability available from the beginning?")]
		public bool startAbility = false;

		[SerializeField] protected bool showInStore = true;
		public bool ShowInStore => showInStore;

		[SerializeField] protected bool showInActions = true;
		public bool ShowInActions => showInActions;

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
				Loggy.LogError($"No ability component found on {abilityObject} for {this}!", Category.Changeling);
				return default;
			}
			abilityComponent.ability = this;
			abilityComponent.CooldownTime = cooldown;
			return abilityComponent;
		}

		public virtual bool UseAbilityClient(ChangelingMain changeling)
		{
			return true;
		}

		[Server]
		public virtual bool UseAbilityServer(ChangelingMain changeling, Vector3 clickPosition)
		{
			changeling.UseAbility(this);
			return true;
		}
	}
}