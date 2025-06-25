using System;
using System.Collections;
using System.Collections.Generic;
using Communications;
using Core.Physics;
using Messages.Server.SoundMessages;
using UnityEngine;
using Objects.Machines;
using ScriptableObjects;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_MaterialSilo : NetTab
	{
		[SerializeField]
		private GUI_MaterialsList materialsListDisplay = null;

		private ChatEvent chatEvent = new ChatEvent();
		private const ChatChannel ChatChannels = ChatChannel.Common;

		public bool DispensingCash = false;

		public void Start()
		{
			chatEvent.channels = ChatChannels;
			chatEvent.VoiceLevel = Loudness.LOUD;
			chatEvent.position = Provider.gameObject.AssumedWorldPosServer();
			chatEvent.originator = Provider;
			chatEvent.speaker = "[Material Silo]";
		}

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
			materialsListDisplay.materialStorageLink = Provider.GetComponent<MaterialStorageLink>();
			materialsListDisplay.materialStorageLink.materialListGUI = materialsListDisplay;
			materialsListDisplay.UpdateMaterialList();
		}

		public void DoDispenseMoney()
		{
			DispensingCash = !DispensingCash;
			if (DispensingCash)
			{
				Provider.GetComponent<UniversalObjectPhysics>().StartCoroutine(DispenseMoney());
			}
			else
			{
				Chat.AddCommMsgByMachineToChat(Provider.gameObject, $"Cargo's Leaky bank account has stopped leaking", ChatChannel.Local |ChatChannel.Common , Loudness.LOUD);
			}
		}

		public IEnumerator DispenseMoney()
		{
			Chat.AddCommMsgByMachineToChat(Provider.gameObject, $"Cargo's bank account is being drained at the ore Silo", ChatChannel.Local |ChatChannel.Common , Loudness.LOUD);

			_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementAlert);

			while (CargoManager.Instance.Credits > 600 && DispensingCash)
			{
				Spawn.ServerPrefab(CommonPrefabs.Instance.Cash500,
					Provider.gameObject.AssumedWorldPosServer());
				SoundManager.PlayNetworkedAtPos(
					CommonSounds.Instance.Sparks, Provider.gameObject.AssumedWorldPosServer(), new AudioSourceParameters()
				{
					Loops = false,
					SpatialBlend = 2
				});
				CargoManager.Instance.Credits -= 500;
				yield return WaitFor.Seconds(10);
			}

			DispensingCash = false;

		}
	}
}
