using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightSource : Photon.PunBehaviour
	{
		private LightingSourceManager lightSourceManager;
		private SpriteRenderer thisSprite;
		private LightTile contactLightTile;
		[Header("Between 0 - 100")]
		public float brightness = 80f;
		[Header("How many tiles to cover")]
		public int range = 5;
		private float brightCache;

		public Sprite lightOn;
		public Sprite lightOff;

		void Awake()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
			lightSourceManager = GetComponentInParent<LightingSourceManager>();
			brightCache = brightness;
		}

		void Start(){
			StartCoroutine(InitLights());
		}

		IEnumerator InitLights(){
			yield return new WaitForSeconds(0.5f);
			if (lightSourceManager != null) {
				lightSourceManager.UpdateRoomBrightness(this);
			} else {
				Debug.Log("NO LIGHT SOURCE MANAGER");
			}
		}

		public void TurnOnLight(){
			photonView.RPC("OnLight", PhotonTargets.All, null);
		}

		public void TurnOffLight(){
			photonView.RPC("OffLight", PhotonTargets.All, null);
		}

		[PunRPC]
		public void OnLight(){
			brightness = brightCache;
			lightSourceManager.UpdateRoomBrightness(this);
			thisSprite.sprite = lightOn;
		}

		[PunRPC]
		public void OffLight(){
			thisSprite.sprite = lightOff;
			brightness = 0f;
			lightSourceManager.UpdateRoomBrightness(this);
		}

	}
}