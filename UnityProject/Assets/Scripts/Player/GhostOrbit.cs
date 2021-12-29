using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace Player
{
	public class GhostOrbit : MonoBehaviour
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
				FindObjectToOrbitUnderMouse();
			}
			if(target == null) return;
			if (KeyboardInputManager.IsMovementPressed())
			{
				StopOrbiting();
			}
		}

		private void FindObjectToOrbitUnderMouse()
		{
			var stuff = MouseUtils.GetOrderedObjectsUnderMouse();
			foreach (var possibleTarget in stuff)
			{
				if (possibleTarget.TryGetComponent<PushPull>(out var pull))
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

		public void Orbit(GameObject thingToOrbit)
		{
			target = thingToOrbit;
			rotateTransform.TransformToRotateAround = thingToOrbit.transform;
			netTransform.SetPosition(target.AssumedWorldPosServer(), false);
			UpdateManager.Add(FollowTarget, 0.1f);
			Chat.AddExamineMsg(PlayerManager.LocalPlayer, $"You start orbiting {thingToOrbit.ExpensiveName()}");
		}

		private void StopOrbiting()
		{
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
