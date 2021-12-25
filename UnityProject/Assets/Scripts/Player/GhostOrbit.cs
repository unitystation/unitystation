using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Player
{
	public class GhostOrbit : MonoBehaviour
	{
		private GameObject target;
		[SerializeField] private PlayerSync netTransform;
		[SerializeField] private RotateAroundTransform rotateTransform;
		[SerializeField] private Transform spriteTransform;
		private int mouseClickCount = 0;

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
				mouseClickCount += 2;
			}
			if (mouseClickCount >= 4)
			{
				var objectsList = MouseUtils.GetOrderedObjectsUnderMouse();
				foreach (var obj in objectsList)
				{
					if (obj.TryGetComponent<PushPull>(out var pushPull))
					{
						target = obj;
						break;
					}
				}
				mouseClickCount = 0;
			}
			if(mouseClickCount > 0) mouseClickCount--;
			if(target == null) return;
			if (KeyboardInputManager.IsMovementPressed())
			{
				StopOrbiting();
			}
		}

		private void OnDisable()
		{
			StopOrbiting();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		public void Orbit(GameObject thingToOrbit)
		{
			target = thingToOrbit;
			rotateTransform.TransformToRotateAround = thingToOrbit.transform;
			UpdateManager.Add(FollowTarget, 0.1f);
		}

		private void StopOrbiting()
		{
			target = null;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FollowTarget);
			rotateTransform.TransformToRotateAround = null;
			spriteTransform.localEulerAngles = Vector3.zero;
		}

		private void FollowTarget()
		{
			if (target == null) return;
			netTransform.SetPosition(target.AssumedWorldPosServer(), false);
		}

	}

}
