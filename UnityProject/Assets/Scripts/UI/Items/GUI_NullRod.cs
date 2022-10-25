using System;
using System.Collections;
using UnityEngine;

namespace UI.Items
{
	public class GUI_NullRod : NetTab
	{
		[SerializeField]
		private GameObject[] rodTransforms = null;


		private void Awake()
		{
			if (IsMasterTab)
			{
				OnTabOpened.AddListener(newPeeper => { });
			}
		}

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
		}

		// Close the screen.
		public void CloseDialog()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		// Transform the item by deleting it and spawning something in the same hand.
		public void ServerSwapItem(int index)
		{
			CloseDialog();
			Provider.GetComponent<NullRod>().SwapItem(rodTransforms[index]);
		}
	}
}
