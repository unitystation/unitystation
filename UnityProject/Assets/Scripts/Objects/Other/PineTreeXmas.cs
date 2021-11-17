using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using ScriptableObjects.TimedGameEvents;
using UnityEngine;

namespace Objects.Other
{
	public class PineTreeXmas : MonoBehaviour
	{
		[SerializeField] private SpriteDataSO xmasSpriteSO;
		[SerializeField] private TimedGameEventSO eventData;

		private bool canPickUpGifts;

		private SpriteHandler spriteHandler;

		private IEnumerator Start()
		{
			yield return WaitFor.Seconds(12);
			spriteHandler = GetComponent<SpriteHandler>();
			if (SingletonManager<TimedEventsManager>.Instance.ActiveEvents.Contains(eventData))
			{
				canPickUpGifts = true;
				spriteHandler.SetSpriteSO(xmasSpriteSO);
			}
		}
	}
}

