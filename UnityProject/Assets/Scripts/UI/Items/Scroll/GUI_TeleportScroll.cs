using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Scrolls.TeleportScroll;

namespace UI.Scroll
{
	public class GUI_TeleportScroll : NetTab
	{
		[SerializeField]
		private NetLabel chargesLabel = default;
		[SerializeField]
		private EmptyItemList dynamicList = null;

		private ScrollOfTeleportation scroll;

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

			scroll = Provider.GetComponent<ScrollOfTeleportation>();
			UpdateChargesCount();
			GenerateEntries();
		}

		#endregion Lifecycle

		public void TeleportTo(TeleportDestination destination)
		{
			scroll.TeleportTo(destination);

			UpdateChargesCount();
			ServerCloseTabFor(scroll.GetLastReader());
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
			if (scroll.ChargesRemaining < 0)
			{
				chargesLabel.SetValueServer("");
			}
			else
			{
				chargesLabel.SetValueServer($"Teleport Charges: {scroll.ChargesRemaining}");
			}
		}
	}
}
