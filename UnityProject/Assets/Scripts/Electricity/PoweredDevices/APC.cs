using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


	public class APC : NetworkBehaviour
	{
		public PoweredDevice poweredDevice;

		Sprite[] loadedScreenSprites;

		public Sprite[] redSprites;
		public Sprite[] blueSprites;
		public Sprite[] greenSprites;

		public Sprite deadSprite;

		public SpriteRenderer screenDisplay;

		private bool batteryInstalled = true;
		private bool isScreenOn = true;

		private int charge = 10; //charge percent
		private int displayIndex = 0; //for the animation

		private Coroutine coScreenDisplayRefresh;

		public float  Resistance = 240;
		public PowerTypeCategory ApplianceType = PowerTypeCategory.APC;
		public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>();

		//green - fully charged and sufficient power from wire
		//blue - charging, sufficient power from wire
		//red - running off internal battery, not enough power from wire

		public override void OnStartClient()
		{
			base.OnStartClient();
			poweredDevice.CanConnectTo = CanConnectTo;
			poweredDevice.Categorytype = ApplianceType;
			poweredDevice.PassedDownResistance = Resistance;
		//Logger.Log ("Resistance as in model" +poweredDevice.PassedDownResistance.ToString (), Category.Electrical);
			poweredDevice.OnSupplyChange.AddListener(SupplyUpdate);
			if (coScreenDisplayRefresh == null)
				coScreenDisplayRefresh = StartCoroutine(ScreenDisplayRefresh());
		}

		private void OnDisable()
		{
			poweredDevice.OnSupplyChange.RemoveListener(SupplyUpdate);
			if (coScreenDisplayRefresh != null) {
				StopCoroutine(coScreenDisplayRefresh);
				coScreenDisplayRefresh = null;
			}
		}

		//Called whenever the PoweredDevice updates
		void SupplyUpdate(){
			UpdateDisplay();
		}

		void UpdateDisplay(){
			if (poweredDevice.suppliedElectricity.current == 0 && charge == 0){
				loadedScreenSprites = null; // dead
			}
			if (poweredDevice.suppliedElectricity.current > 10 && charge > 0 && charge < 98) {
				loadedScreenSprites = blueSprites;
			}
			if (poweredDevice.suppliedElectricity.current > 10 && charge >= 98) {
				loadedScreenSprites = greenSprites;
			}
			if (poweredDevice.suppliedElectricity.current < 10 && charge > 0) {
				loadedScreenSprites = redSprites;
			}
		}

		IEnumerator ScreenDisplayRefresh(){
			yield return new WaitForEndOfFrame();
			while(true) {
				if (loadedScreenSprites == null)
					screenDisplay.sprite = deadSprite;
				else {
					if (++displayIndex >= loadedScreenSprites.Length) {
						displayIndex = 0;
					}
					screenDisplay.sprite = loadedScreenSprites[displayIndex];
				}
				yield return new WaitForSeconds(3f);
			}
		}
	}
