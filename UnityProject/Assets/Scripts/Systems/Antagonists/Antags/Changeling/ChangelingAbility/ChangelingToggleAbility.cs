using Changeling;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
{
	public class ChangelingToggleAbility : ChangelingBaseAbility
	{
		[SerializeField] private bool swithedToOnWhenInCrit = false;
		public bool SwithedToOnWhenInCrit => swithedToOnWhenInCrit;

		[SerializeField] private bool swithedToOffWhenExitCrit = false;
		public bool SwithedToOffWhenExitCrit => swithedToOffWhenExitCrit;

		[Tooltip("Activats cooldown when ability is toggled anytime. Not after ability is toggled off only")]
		[SerializeField] private bool cooldownWhenToggled = false;
		public bool CooldownWhenToggled => cooldownWhenToggled;


		public virtual bool UseAbilityToggleClient(ChangelingMain changeling, bool toggle)
		{
			return true;
		}

		public virtual bool UseAbilityToggleServer(ChangelingMain changeling, bool toggle)
		{
			return true;
		}
	}
}