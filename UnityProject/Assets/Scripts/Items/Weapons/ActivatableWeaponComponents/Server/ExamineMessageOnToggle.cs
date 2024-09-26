using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class ExamineMessageOnToggle : ServerActivatableWeaponComponent
	{
		public string activateMessage;
		public string deactivateMessage;

		public override void ServerActivateBehaviour(GameObject performer)
		{
			Chat.AddExamineMsgFromServer(performer, activateMessage.Replace("{obj}", gameObject.ExpensiveName()));
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			Chat.AddExamineMsgFromServer(performer, deactivateMessage.Replace("{obj}", gameObject.ExpensiveName()));
		}
	}
}