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

		private void Start()
		{
			if (netTransform == null) netTransform = GetComponent<PlayerSync>();
		}

		public void Orbit(GameObject thingToOrbit)
		{
			target = thingToOrbit;
			UpdateManager.Add(FollowTarget, 0.1f);
		}

		public void StopOrbiting()
		{
			target = null;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FollowTarget);
		}

		private void FollowTarget()
		{
			if (target == null) return;
			netTransform.SetPosition(target.WorldPosServer(), false);
		}

	}

}
