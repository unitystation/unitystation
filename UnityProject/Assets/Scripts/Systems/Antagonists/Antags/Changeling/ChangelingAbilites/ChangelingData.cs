using ScriptableObjects.Systems.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Spells;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Abilities/Base")]
	public class ChangelingData : ActionData, ICooldown
	{
		public short Index => (short)ChangelingAbilityList.Instance.Abilites.IndexOf(this);

		public override bool CallOnClient => true;
		public override bool CallOnServer => false;

		[Header("Variables")]
		// ep - evolution point
		[Tooltip("Evolution points cost for buying")]
		[SerializeField] private int abilityEPCost;
		[Tooltip("Chemical points cost for use")]
		[SerializeField] private int abilityChemCost;
		[Tooltip("Is action can be reseted")]
		public bool canBeReseted = false;
		[Tooltip("Is ability will be added on start")]
		public bool startAbility = false;
		[Tooltip("Is ability will be used when added to changeling")]
		public bool abilityUsedOnStart = false;
		[Tooltip("Is ability will be used only one time")]
		public bool singleUseAbility = false;

		[SerializeField] private int cooldown = 1;
		public float DefaultTime => cooldown;


		public GameObject AbilityImplementation => abilityImplementation;
		[Tooltip("Implementation prefab, defaults to SimpleSpell if null")]
		[SerializeField] private GameObject abilityImplementation = null;

		/// <summary>
		/// Perfoms current ability.
		/// </summary>
		/// <param name="changeling"></param>
		/// <param name="objToPerfom"></param>
		/// <returns>Succeseded of perfoming</returns>
		public virtual bool PerfomAbility(ChangelingMain changeling, dynamic objToPerfom)
		{
			return true;
		}

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
			var spellObject = Instantiate(AbilityImplementation, player.gameObject.transform);
			var spellComponent = spellObject.GetComponent<ChangelingAbility>();
			if (spellComponent == null)
			{
				Logger.LogError($"No ability component found on {spellObject} for {this}!", Category.Changeling);
				return default;
			}
			spellComponent.ability = this;
			//spellComponent.CooldownTime = CooldownTime;
			return spellComponent;
		}
	}
}