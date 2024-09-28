using Core.Physics;
using System.Collections;
using UnityEngine;
using Logs;


namespace Objects.Traps
{
	[RequireComponent(typeof(GenericTriggerOutput))]
	public class PressurePlate : EnterTileBase
	{
		private GenericTriggerOutput _output;

		[SerializeField] private float pressDuration = 3f;
		[SerializeField] private SpriteHandler spriteHandler;

		private RegisterObject registerObject;
		private const int IDLE_VARIANT_INDEX = 0;
		private const int PRESSED_VARIANT_INDEX = 1;

		protected override void Awake()
		{
			_output = GetComponent<GenericTriggerOutput>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			registerObject = GetComponent<RegisterObject>();
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			OnObjectEnter(playerScript.gameObject);
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			_output.TriggerOutput();
			spriteHandler.SetSpriteVariant(PRESSED_VARIANT_INDEX);
			StopAllCoroutines();
			StartCoroutine(WaitToRelease());
		}

		private IEnumerator WaitToRelease()
		{
			for (;;)
			{
				if (IsObjectPresent() == true) yield return new WaitForSeconds(pressDuration);
				else break;
			}

			_output.ReleaseOutput();
			spriteHandler.SetSpriteVariant(IDLE_VARIANT_INDEX);
		}

		private bool IsObjectPresent()
		{
			foreach (var reg in registerObject.Matrix.Get(registerObject.LocalPositionServer, isServer))
			{
				if (reg.gameObject == gameObject) continue;
				if (reg.ObjectPhysics.Component == null)
				{
					Loggy.LogError(reg.name + " Does not have object physics");
					continue;
				}
				if (reg.ObjectPhysics.Component.Intangible) continue;

				return true;
			}
			return false;
		}

		protected override void OnDisable()
		{
			StopAllCoroutines();
			objectPhysics.OnLocalTileReached.RemoveListener(OnLocalPositionChangedServer);
		}
	}
}
