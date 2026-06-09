using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Managers;
using US13.Managers.NetworkManagement;

namespace US13.UI.Core.Net.Elements
{
	public class NetUIChildActive : NetUIStringElement
	{

		public GameObject ToToggleChild;

		public bool MakeChildrenInvisible;

		private Dictionary<Image, Color> ImageColourDefault = new Dictionary<Image, Color>();
		private Dictionary<TMP_Text, Color> TMP_TextColourDefault = new Dictionary<TMP_Text, Color>();

		private bool Init = false;

		private string State = "1";


		public override string Value
		{
			get
			{

				if (MakeChildrenInvisible)
				{
					return State;
				}
				else
				{

					return ToToggleChild.activeSelf ? "1" : "0";
				}
			}
			protected set {
				externalChange = true;
				if (MakeChildrenInvisible)
				{
					State = value;
					SetChildren(value.Equals("1"));
				}
				else
				{
					ToToggleChild.SetActive(value.Equals("1"));
				}

				externalChange = false;
				NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			}
		}

		[SerializeField]
		[InfoBox("If the toggle is part of a toggle group, and the toggles point to the same listeners below, " +
		         "then they will be hit multiple times (each toggle, on / off). This is often not desirable. " +
		         "A workaround is to only invoke the listener if the toggle is on, so the listener is only called once. " +
		         "Check 'Enable Workaround' to enable this behaviour. ", EInfoBoxType.Normal)]
		// enough hours wasted on falling for the same mistake again and again... my darkest hours with that damned pipe dispenser
		private bool enableWorkaround = false;

		public BoolEvent ServerMethod;
		public BoolEventWithSubject ServerMethodWithSubject;

		public void SetChildren(bool Enable )
		{
			if (Init == false)
			{
				var Images = GetComponentsInChildren<Image>();
				var TMP_Texts = GetComponentsInChildren<TMP_Text>();

				foreach (var Image in Images)
				{
					ImageColourDefault[Image] = Image.color;
				}

				foreach (var Texts in TMP_Texts)
				{
					TMP_TextColourDefault[Texts] = Texts.color;
				}

				Init = true;
			}

			if (Enable)
			{
				foreach (var Image in ImageColourDefault)
				{
					Image.Key.color = Image.Value;
				}

				foreach (var tmp in TMP_TextColourDefault)
				{
					tmp.Key.color = tmp.Value;
				}
			}
			else
			{
				foreach (var Image in ImageColourDefault)
				{
					Image.Key.color= Color.clear;
				}

				foreach (var tmp in TMP_TextColourDefault)
				{
					tmp.Key.color = Color.clear;
				}
			}
		}

		public override void ExecuteServer(PlayerInfo subject)
		{

		}

		public override void ExecuteClient()
		{
			if (enableWorkaround && ToToggleChild.activeSelf == false) return;
			base.ExecuteClient();
		}

		public void MasterNetSetActive(bool activity)
		{
			MasterSetValue(activity ? "1" : "0");

		}
	}
}
