using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class FieldGenerator : InputTrigger
	{
		public PoweredDevice poweredDevice;

		[SyncVar(hook = "CheckState")]
		public bool isOn = false;
		public bool connectedToOther = false;
		private Coroutine coSpriteAnimator;

		public Sprite offSprite;
		public Sprite onSprite;
		public Sprite[] searchingSprites;
		public Sprite[] connectedSprites;

		public SpriteRenderer spriteRend;

		List<Sprite> animSprites = new List<Sprite>();


		public override void OnStartClient()
		{
			base.OnStartClient();
			poweredDevice.OnSupplyChange.AddListener(SupplyUpdate);
			CheckState(isOn);
		}

		private void OnDisable()
		{
			poweredDevice.OnSupplyChange.RemoveListener(SupplyUpdate);
		}

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			if (!isServer) {
				InteractMessage.Send(gameObject, hand);
			} else {
				isOn = !isOn;
				CheckState(isOn);
			}
		}

		//Power supply updates
		void SupplyUpdate(){
			CheckState(isOn);
		}

		//Check the operational state
		void CheckState(bool _isOn){
			if(isOn){
				if(poweredDevice.suppliedElectricity.current == 0){
					if (coSpriteAnimator != null) {
						StopCoroutine(coSpriteAnimator);
						coSpriteAnimator = null;
					}
					spriteRend.sprite = onSprite;
				}
				if(poweredDevice.suppliedElectricity.current > 15){
					if(!connectedToOther){
						animSprites = new List<Sprite>(searchingSprites);
						if (coSpriteAnimator == null) {
							coSpriteAnimator = StartCoroutine(SpriteAnimator());
						}
					} else {
						animSprites = new List<Sprite>(connectedSprites);
						if(coSpriteAnimator == null) {
							coSpriteAnimator = StartCoroutine(SpriteAnimator());
						}
					}
				}
			} else {
				if (coSpriteAnimator != null) {
					StopCoroutine(coSpriteAnimator);
					coSpriteAnimator = null;
				}
				spriteRend.sprite = offSprite;
			}
		}

		IEnumerator SpriteAnimator(){
			int index = 0;
			while(true){
				Debug.Log("animating shield");
				if(index >= animSprites.Count){
					index = 0;
				}
				spriteRend.sprite = animSprites[index];
				index++;
				yield return new WaitForSeconds(0.3f);
			}
		}
	}

