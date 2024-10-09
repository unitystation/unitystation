using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	[RequireComponent(typeof(ActivatableWeapon))]
	public abstract class ServerActivatableWeaponComponent : MonoBehaviour
	{
		protected ActivatableWeapon av;

		private void Awake()
		{
			av = GetComponent<ActivatableWeapon>();
			av.ServerOnActivate += ServerActivateBehaviour;
			av.ServerOnDeactivate += ServerDeactivateBehaviour;
		}

		private void OnDestroy()
		{
			av.ServerOnActivate -= ServerActivateBehaviour;
			av.ServerOnDeactivate -= ServerDeactivateBehaviour;
		}

		public abstract void ServerActivateBehaviour(GameObject performer);
		public abstract void ServerDeactivateBehaviour(GameObject performer);
	}
}