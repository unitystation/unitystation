using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Electricity
{
	public class APC : NetworkBehaviour
	{
		public PoweredDevice poweredDevice;

		List<Sprite> loadedScreenSprites = new List<Sprite>();

		public Sprite[] redSprites;
		public Sprite[] blueSprites;
		public Sprite[] greenSprites;

		public Sprite deadSprite;

		public SpriteRenderer screenDisplay;

		private bool batteryInstalled = true;
		private bool isScreenOn = true;

		private int charge = 10; //charge percent
		private int displayIndex = 0; //for the animation

		//green - fully charged and sufficient power from wire
		//blue - charging, sufficient power from wire
		//red - running off internal battery, not enough power from wire

		public override void OnStartClient()
		{
			base.OnStartClient();
			loadedScreenSprites.Add(deadSprite);
			poweredDevice.OnSupplyChange.AddListener(SupplyUpdate);
			StartCoroutine(ScreenDisplayRefresh());
		}

		private void OnDisable()
		{
			poweredDevice.OnSupplyChange.RemoveListener(SupplyUpdate);
			StopCoroutine(ScreenDisplayRefresh());
		}

		//Called whenever the PoweredDevice updates
		void SupplyUpdate(){
			UpdateDisplay();
		}

		void UpdateDisplay(){
			loadedScreenSprites.Clear();
			if (poweredDevice.suppliedElectricity.current == 0 && charge == 0){
				loadedScreenSprites.Add(deadSprite);
			}
			if (poweredDevice.suppliedElectricity.current > 10 && charge > 0 && charge < 98) {
				loadedScreenSprites = new List<Sprite>(blueSprites);
			}
			if (poweredDevice.suppliedElectricity.current > 10 && charge >= 98) {
				loadedScreenSprites = new List<Sprite>(greenSprites);
			}
			if (poweredDevice.suppliedElectricity.current < 10 && charge > 0) {
				loadedScreenSprites = new List<Sprite>(redSprites);
			}
		}

		IEnumerator ScreenDisplayRefresh(){
			yield return new WaitForEndOfFrame();
			while(true){
				displayIndex++;
				if(displayIndex >= loadedScreenSprites.Count){
					displayIndex = 0;
				}
				screenDisplay.sprite = loadedScreenSprites[displayIndex];
				yield return new WaitForSeconds(3f);
			}
		}
	}
}