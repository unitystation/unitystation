using System;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnLayDown : ProtipObject
	{
		[SerializeField] private PlayerScript playerScript;

		protected override void Awake()
		{
			if (playerScript == null) playerScript = GetComponentInParent<PlayerScript>();
			playerScript.OnLayDown += OnLayDown;
			base.Awake();
		}

		private void OnDestroy()
		{
			if (playerScript != null) playerScript.OnLayDown -= OnLayDown;
		}

		private void OnLayDown()
		{
			TriggerTip();
		}
	}
}