using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	[RequireComponent(typeof(ActivatableWeapon))]
	public abstract class ClientActivatableWeaponComponent : MonoBehaviour
	{
		protected ActivatableWeapon av;

		private void Awake()
		{
			av = GetComponent<ActivatableWeapon>();
			av.ClientOnActivate += ClientActivateBehaviour;
			av.ClientOnDeactivate += ClientDeactivateBehaviour;
		}

		private void OnDestroy()
		{
			av.ClientOnActivate -= ClientActivateBehaviour;
			av.ClientOnDeactivate -= ClientDeactivateBehaviour;
		}

		public abstract void ClientActivateBehaviour();
		public abstract void ClientDeactivateBehaviour();
	}
}