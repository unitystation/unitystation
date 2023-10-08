using System;
using System.Collections;
using Light2D;
using Logs;
using Mirror;
using UnityEngine;

namespace Systems.Radiation
{
	public class RadiationProducer : NetworkBehaviour
	{
		public float OutPuttingRadiation = 0;
		public Color color = new Color(93f / 255f, 202 / 255f, 49 / 255f, 0);
		private GameObject mLightRendererObject;
		private UniversalObjectPhysics objectBehaviour;
		private RegisterObject registerObject;
		[NonSerialized] public int ObjectID = 0;
		private LightSprite lightSprite;
		public Sprite DotSprite;


		[SyncVar(hook = nameof(SynchStrength))]
		public float SynchroniseStrength = 0;

		private void SynchStrength(float old, float newv)
		{
			if (old != newv)
			{
				SynchroniseStrength = newv;
				UpdateValues(SynchroniseStrength);
			}
		}


		private void Awake()
		{
			//yeah dam Unity initial Conditions  is not updating
			color = new Color(93f / 255f, 202 / 255f, 49 / 255f, 0);

			objectBehaviour = this.GetComponent<UniversalObjectPhysics>();
			ObjectID = this.GetInstanceID();

			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, color, 7);
			mLightRendererObject.SetActive(true);

			lightSprite = mLightRendererObject.GetComponent<LightSprite>();
			lightSprite.Sprite = DotSprite;
			registerObject = this.GetComponent<RegisterObject>();
		}


		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(RequestPulse, 5);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RequestPulse);
		}

		/*private IEnumerator Refresh()
		{
			//Request pulse
			yield return WaitFor.Seconds(5f);
			if (OutPuttingRadiation > 0)
			{
				StartCoroutine(Refresh());
			}
		}*/

		public void SetLevel(float Invalue)
		{
			SynchStrength(SynchroniseStrength, Invalue);
		}

		private void UpdateValues(float Invalue)
		{
			if (this == null)
			{
				Loggy.LogError(
					" The radioactive object has been destroyed but you're still trying to Produce radiation ",
					Category.Radiation);
				return;
			}

			OutPuttingRadiation = Invalue;
			float LightPower = OutPuttingRadiation / 24000;
			if (LightPower > 1)
			{
				mLightRendererObject.transform.localScale = Vector3.one * (7 * LightPower);
				LightPower = 1;
			}

			lightSprite.Color.a = LightPower;
		}

		private void RequestPulse()
		{
			if (OutPuttingRadiation > 0.358f)
			{
				RadiationManager.Instance.RequestPulse(gameObject.AssumedWorldPosServer().RoundToInt(),
					OutPuttingRadiation,
					ObjectID);
			}

			UpdateValues(OutPuttingRadiation);

			//Logger.Log("RequestPulse!!" + Time.time);
		}
	}
}