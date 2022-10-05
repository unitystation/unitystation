using System.Collections;
using UI.Core.NetUI;
using UnityEngine;
using Systems.Cargo;

namespace Objects.TGMC
{
	public class GUI_ASRSField : NetTab
	{
		[SerializeField]
		private EmptyItemList beaconList = null;

		private ASRSFieldTeleporter teleporterMain;

		private ASRSBeacon beacon;

		internal delegate void deselectAll (bool selected);
		internal deselectAll setEntriesSelect = null;

		[SerializeField]
		private NetText_label CreditsText = null;
		[SerializeField]
		private NetText_label CostText = null;

		private float lastActivationTime;
		private float TimePassed
		{
			get
			{
				return Time.time - lastActivationTime;
			}
		}


		public override void OnEnable()
		{
			base.OnEnable();
			OnTabOpened.AddListener(PlayerJoinsTab);
		}

		private void OnDisable()
		{
			OnTabOpened.RemoveListener(PlayerJoinsTab);
		}


		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
			CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateCreditsText);
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				// waiting for Provider
				yield return WaitFor.EndOfFrame;
			}

			teleporterMain = Provider.GetComponent<ASRSFieldTeleporter>();

			if (teleporterMain != null) PlayerJoinsTab();
		}

		private void UpdateCreditsText()
		{
			string credits = CargoManager.Instance.Credits.ToString();
			CreditsText.SetValueServer($" Availiable Credits: {credits}");
		}

		private void PlayerJoinsTab(PlayerInfo newPeeper = default)
		{
			if (teleporterMain == null)
			{
				StartCoroutine(WaitForProvider());
				return;
			}

			UpdateCreditsText();

			string cost = teleporterMain.RequiredCredits.ToString();

			CostText.SetValueServer($" Teleport Cost: {cost} credits");

			var beacons = ASRSBeacon.GetActiveBeacons();

			if (beacons.Count != beaconList.Entries.Length)
			{
				beaconList.SetItems(beacons.Count);
			}

			setEntriesSelect = null;

			int i = 0;
			foreach(var beacon in beacons)
			{
				DynamicEntry dynamicEntry = beaconList.Entries[i];
				var entry = dynamicEntry.GetComponent<ASRSBeaconEntry>();
				entry.SetValues(this, beacon);

				i++;
			}

		}

		public void OnEntryPressed(ASRSBeacon beaconVar)
		{
			setEntriesSelect?.Invoke(false);

			beacon = beaconVar;
		}

		public void onSendButtonPressed()
		{
			OnTeleport();
		}

		public void OnTeleport()
		{
			if(teleporterMain == null || beacon == null) return;

			int creditsNeeded = teleporterMain.RequiredCredits;
			if(creditsNeeded > CargoManager.Instance.Credits || TimePassed < teleporterMain.CoolDown)
			{
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.AccessDenied, teleporterMain.gameObject.RegisterTile().WorldPositionServer);
				return;
			}

			CargoManager.Instance.SpendCredits(creditsNeeded);

			lastActivationTime = Time.time;

			Vector3 beaconPos = beacon.CurrentBeaconPosition();

			teleporterMain.TeleportToBeacon(beaconPos);
		}
	}
}
