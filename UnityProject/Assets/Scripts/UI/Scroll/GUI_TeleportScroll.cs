using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Scrolls.TeleportScroll;

namespace UI.Scroll
{
	public class GUI_TeleportScroll : NetTab
	{
		[Tooltip("Assign the uses remaining label here.")]
		[SerializeField]
		private NetLabel chargesLabel = default;

		[Tooltip("Assign the teleport destination entry template here.")]
		[SerializeField]
		private EmptyItemList dynamicList = null;

		public ScrollOfTeleportation Scroll { get; private set; }

		#region Lifecycle

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			Scroll = Provider.GetComponent<ScrollOfTeleportation>();
			UpdateChargesCount();
			GenerateEntries();
		}

		#endregion Lifecycle

		public void TeleportTo(TeleportDestination destination)
		{
			Scroll.TeleportTo(destination);

			UpdateChargesCount();
			CloseTab(); // TODO: does not close clientside.
		}

		private void GenerateEntries()
		{
			var destinations = Enum.GetValues(typeof(TeleportDestination));

			dynamicList.AddItems(destinations.Length);
			for (int i = 0; i < destinations.Length; i++)
			{
				var destination = (TeleportDestination)destinations.GetValue(i);
				dynamicList.Entries[i].GetComponent<GUI_TeleportScrollEntry>().Init(this, destination);
			}
		}

		private void UpdateChargesCount()
		{
			if (Scroll.ChargesRemaining < 0)
			{
				chargesLabel.SetValueServer("");
			}
			else
			{
				chargesLabel.SetValueServer($"Teleport Charges: {Scroll.ChargesRemaining}");
			}
		}
	}
}
