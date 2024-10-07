using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class DeactivateOnInventoryMove : ServerActivatableWeaponComponent, IServerInventoryMove
	{
		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (gameObject != info.MovedObject.gameObject) return;
			if (av.IsActive == false) return; 
			if (info.InventoryMoveType == InventoryMoveType.Remove || info.InventoryMoveType == InventoryMoveType.Transfer )
			{
				av.ServerOnDeactivate?.Invoke(info.FromPlayer.gameObject);
				av.SyncState(av.IsActive, false);
			}
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			//
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			//
		}
	}
}