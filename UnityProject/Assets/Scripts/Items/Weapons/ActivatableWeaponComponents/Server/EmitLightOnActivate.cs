using System.Collections;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	[RequireComponent(typeof(ItemLightControl))]
	public class EmitLightOnActivate : ServerActivatableWeaponComponent
	{
		[SerializeField] private ItemLightControl lightControl;

		[SerializeField] public Color Color;

		private void Start()
		{
			lightControl.SetColor(Color);
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			lightControl.Toggle(true);
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			StartCoroutine(SendUpdateMsg());
		}

		private IEnumerator SendUpdateMsg ()
		{
			yield return new WaitForEndOfFrame();
			lightControl.Toggle(false);
		}

	}
}