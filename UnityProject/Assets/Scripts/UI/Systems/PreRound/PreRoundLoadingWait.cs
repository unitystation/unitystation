using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.PreRound
{
	public class PreRoundLoadingWait : MonoBehaviour
	{

		[SerializeField] private Image loadingImage = null;
		[SerializeField] private GUI_PreRoundWindow win = null;
		[SerializeField] private List<Sprite> loadingSprites = new();
		[SerializeField] private int loadingTimeOut = 124;

		private int currentTick = 0;

		private void Awake()
		{
			loadingImage ??= GetComponentInChildren<Image>();
		}

		private void OnEnable()
		{
			currentTick = 0;
			UpdateManager.Add(UpdateMe, 1.25f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			loadingImage.sprite = loadingSprites.PickRandom();
			currentTick++;

			if (currentTick >= loadingTimeOut)
			{
				win.SwitchToMainPage();
			}
		}
	}
}