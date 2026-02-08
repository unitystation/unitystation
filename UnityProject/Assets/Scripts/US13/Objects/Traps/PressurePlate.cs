using System.Collections;
using Logs;
using UnityEngine;
using UnityEngine.Events;
using US13.Core.Physics;
using US13.Core.Sprite_Handler;
using US13.Player;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Objects.Traps
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

		public UnityEvent OnPlayerStepEvent = new UnityEvent();

		protected override void Awake()
		{
			_output = GetComponent<GenericTriggerOutput>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			registerObject = GetComponent<RegisterObject>();
		}

		protected override void OnDisable()
		{
			StopAllCoroutines();
			objectPhysics.OnLocalTileReached.RemoveListener(OnLocalPositionChangedServer);
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			OnObjectEnter(playerScript.gameObject);
			OnPlayerStepEvent?.Invoke();
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
					Loggy.Error(reg.name + " Does not have object physics");
					continue;
				}
				if (reg.ObjectPhysics.Component.Intangible) continue;

				return true;
			}
			return false;
		}
	}
}
