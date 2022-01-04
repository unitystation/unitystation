using System.Threading.Tasks;
using Objects;
using UnityEngine;
using Mirror;


namespace Player
{
	public class GhostOrbit : NetworkBehaviour
	{
		private GameObject target;
		[SerializeField] private PlayerSync netTransform;
		[SerializeField] private RotateAroundTransform rotateTransform;

		/// <summary>
		/// Time in milliseconds! The time between mouse clicks where we can orbit an object
		/// </summary>
		private readonly int doubeClickTime = 500;
		private bool hasClicked = false;

		private void Start()
		{
			if (netTransform == null) netTransform = GetComponent<PlayerSync>();
			if (rotateTransform == null) rotateTransform = GetComponent<RotateAroundTransform>();
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			StopOrbiting();
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}


		private void UpdateMe()
		{
			if (Input.GetMouseButtonDown(0))
			{
				if (hasClicked == false)
				{
					DoubleClickTimer();
					return;
				}
				FindObjectToOrbitUnderMouse();
			}
			if (KeyboardInputManager.IsMovementPressed())
			{
				CmdStopOrbiting();
			}
		}

		private void FindObjectToOrbitUnderMouse()
		{
			var possibleTargets = MouseUtils.GetOrderedObjectsUnderMouse();
			foreach (var possibleTarget in possibleTargets)
			{
				if (possibleTarget.TryGetComponent<PushPull>(out var pull) || possibleTarget.TryGetComponent<Singularity>(out var loose))
				{
					CmdServerOrbit(possibleTarget);
					return;
				}
			}
		}

		private async void DoubleClickTimer()
		{
			hasClicked = true;
			await Task.Delay(doubeClickTime).ConfigureAwait(false);
			hasClicked = false;
		}

		[Server]
		private void Orbit(GameObject thingToOrbit)
		{
			target = thingToOrbit;
			rotateTransform.TransformToRotateAround = thingToOrbit.transform;
			netTransform.SetPosition(target.AssumedWorldPosServer(), false);
			UpdateManager.Add(FollowTarget, 0.1f);
			Chat.AddExamineMsg(gameObject, $"You start orbiting {thingToOrbit.ExpensiveName()}");
		}

		[Server]
		private void StopOrbiting()
		{
			if(target == null) return;
			Chat.AddExamineMsg(gameObject, $"You stop orbiting {target.ExpensiveName()}");
			target = null;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FollowTarget);
			rotateTransform.TransformToRotateAround = null;
			rotateTransform.transform.up = Vector3.zero;
			rotateTransform.transform.localPosition = Vector3.zero;
		}

		[Command]
		public void CmdStopOrbiting()
		{
			if(target == null) return;
			StopOrbiting();
		}

		/// <summary>
		/// Mirror does not support IEnumerable so we cannot turn the FindObjectToOrbit function into a command.
		/// </summary>
		[Command]
		public void CmdServerOrbit(GameObject thingToOrbit)
		{
			Orbit(thingToOrbit);
		}

		private void FollowTarget()
		{
			if (target == null) return;
			netTransform.SetPosition(target.AssumedWorldPosServer(), false);
		}

	}

}
