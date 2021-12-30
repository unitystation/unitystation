using System;
using System.Collections;
using System.Collections.Generic;
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
		private int doubeClickTime = 500;
		private bool hasClicked = false;

		private void Start()
		{
			if (netTransform == null) netTransform = GetComponent<PlayerSync>();
			if (rotateTransform == null) rotateTransform = GetComponent<RotateAroundTransform>();
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
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
				//We get the object list from the client then pass it to the server in the CmdFindObjectToOrbitUnderMouse()
				var possibleTargets = MouseUtils.GetOrderedObjectsUnderMouse();
				CmdFindObjectToOrbitUnderMouse(possibleTargets);
			}
			if(target == null) return;
			if (KeyboardInputManager.IsMovementPressed())
			{
				StopOrbiting();
			}
		}

		[Command(requiresAuthority = false)]
		private void CmdFindObjectToOrbitUnderMouse(IEnumerable<GameObject> objects)
		{
			foreach (var possibleTarget in objects)
			{
				if (possibleTarget.TryGetComponent<PushPull>(out var pull) || possibleTarget.TryGetComponent<Singularity>(out var loose))
				{
					Orbit(possibleTarget);
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

		private void OnDisable()
		{
			StopOrbiting();
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		[Server]
		public void Orbit(GameObject thingToOrbit)
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
			Chat.AddExamineMsg(gameObject, $"You stop orbiting {target.ExpensiveName()}");
			target = null;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FollowTarget);
			rotateTransform.TransformToRotateAround = null;
			rotateTransform.transform.up = Vector3.zero;
			rotateTransform.transform.localPosition = Vector3.zero;
		}

		private void FollowTarget()
		{
			if (target == null) return;
			netTransform.SetPosition(target.AssumedWorldPosServer(), false);
		}

	}

}
