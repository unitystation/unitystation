using System.Collections;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class DeactivateAfterDelay : ServerActivatableWeaponComponent
	{
		[SerializeField] private float delay = 1f;

		private void Start()
		{
			av.canDeactivate.RecordPosition(this, false);
		}

		private void OnDestroy()
		{
			av.canDeactivate.RemovePosition(this);
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			StartCoroutine(AfterDelay(performer));
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			//
		}

		private IEnumerator AfterDelay(GameObject performer)
		{
			yield return new WaitForSeconds(delay);

			if (av.IsActive)
			{
				av.ServerOnDeactivate?.Invoke(performer);
				av.SyncState(av.IsActive, false);
			}
		}
	}
}