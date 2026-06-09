using UnityEngine;

namespace US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility
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

		[Tooltip("Will this ability make chem regeneration slower?")]
		[SerializeField] protected bool isSlowingChemRegeneration = false;
		public bool IsSlowingChemRegeneration => isSlowingChemRegeneration;
		[Tooltip("Will this ability stop chem regeneration while active?")]
		[SerializeField] protected bool isStopingChemRegeneration = false;
		public bool IsStopingChemRegeneration => isStopingChemRegeneration;


		/// <param name="fromServer">When true, server already applied the toggle; only run client visuals, do not send CmdRequestChangelingAbilitesToggle.</param>
		public virtual bool UseAbilityToggleClient(ChangelingMain changeling, bool toggle, bool fromServer = false)
		{
			return true;
		}

		public virtual bool UseAbilityToggleServer(ChangelingMain changeling, bool toggle)
		{
			return true;
		}
	}
}