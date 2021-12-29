using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Systems.Interaction;
using UnityEngine;


namespace Player
{
	public class GhostOrbit : MonoBehaviour, ICheckedInteractable<GhostApply>
	{
		private GameObject target;
		[SerializeField] private PlayerSync netTransform;
		[SerializeField] private RotateAroundTransform rotateTransform;
		[SerializeField] private Transform spriteTransform;

		private int doubleClickTime = 500;
		private GameObject clickedPossibleTarget;

		private void Start()
		{
			if (netTransform == null) netTransform = GetComponent<PlayerSync>();
			if (rotateTransform == null) rotateTransform = GetComponent<RotateAroundTransform>();
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if(target == null) return;
			if (KeyboardInputManager.IsMovementPressed())
			{
				StopOrbiting();
			}
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
			UpdateManager.Add(FollowTarget, 0.1f);
		}

		private void StopOrbiting()
		{
			target = null;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FollowTarget);
			rotateTransform.TransformToRotateAround = null;
			//why wont this shit fucking work
			spriteTransform.localEulerAngles = Vector3.zero;
			spriteTransform.eulerAngles = Vector3.zero;
		}

		private void FollowTarget()
		{
			if (target == null) return;
			netTransform.SetPosition(target.AssumedWorldPosServer(), false);
		}

		private async void DoubleClickCheck(GameObject targetClicked)
		{
			clickedPossibleTarget = targetClicked;
			await Task.Delay(doubleClickTime).ConfigureAwait(false);
			clickedPossibleTarget = null;
		}


		public bool WillInteract(GhostApply interaction, NetworkSide side)
		{
			Debug.Log("test test");
			if (interaction.TargetObject.TryGetComponent<PushPull>(out var pushPull)) return true;
			return false;
		}

		public void ServerPerformInteraction(GhostApply interaction)
		{
			if (clickedPossibleTarget == interaction.TargetObject)
			{
				Orbit(interaction.TargetObject);
				return;
			}
			if (interaction.TargetObject.TryGetComponent<PushPull>(out var pushPull))
			{
				DoubleClickCheck(pushPull.gameObject);
			}
		}
	}

}
