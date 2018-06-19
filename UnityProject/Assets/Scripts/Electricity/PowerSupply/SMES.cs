using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroups.Input;

namespace Electricity
{
	public class SMES : InputTrigger
	{
		public PowerSupply powerSupply;

		//Is the SMES turned on
		[SyncVar(hook="UpdateState")]
		public bool isOn = false;
		public int currentCharge; // 0 - 100

		//Sprites:
		public Sprite offlineSprite;
		public Sprite onlineSprite;
		public Sprite[] chargeIndicatorSprites;
		public Sprite statusCriticalSprite;
		public Sprite statusSupplySprite;

		//Renderers:
		public SpriteRenderer statusIndicator;
		public SpriteRenderer OnOffIndicator;
		public SpriteRenderer chargeIndicator;

		public override void OnStartClient(){
			base.OnStartClient();
			UpdateState(isOn);

			//Test
			currentCharge = 100;
		}

		//Update the current State of the SMES (sprites and statistics) 
		void UpdateState(bool _isOn){
			isOn = _isOn;
			if(isOn){
				//Start the supply of electricity to the circuit:
				powerSupply.TurnOnSupply(3000f, 20f); //Test supply of 3000volts and 20amps

				OnOffIndicator.sprite = onlineSprite;
				chargeIndicator.gameObject.SetActive(true);
				statusIndicator.gameObject.SetActive(true);

				int chargeIndex = (currentCharge / 100) * 4;
				chargeIndicator.sprite = chargeIndicatorSprites[chargeIndex];
				if(chargeIndex == 0){
					statusIndicator.sprite = statusCriticalSprite;
				} else {
					statusIndicator.sprite = statusSupplySprite;
				}
			} else {
				powerSupply.TurnOffSupply(); // Turn off supply to the circuit

				OnOffIndicator.sprite = offlineSprite;
				chargeIndicator.gameObject.SetActive(false);
				statusIndicator.gameObject.SetActive(false);
			}
		}

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			//Interact stuff with the SMES here
			if (!isServer) {
				InteractMessage.Send(gameObject, hand);
			} else {
				isOn = !isOn;
			}
		}

	}
}
