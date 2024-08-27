using System;
using System.Collections;
using Core.Sprite_Handler;
using Light2D;
using Logs;
using Mirror;
using SecureStuff;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Radiation
{
	public class RadiationProducer : NetworkBehaviour
	{
		public float OutPuttingRadiation = 0;
		private Color Colour;
		[FormerlySerializedAs("color")] public Color InitialColour = new Color(93f / 255f, 202 / 255f, 49 / 255f, 0);
		[NonSerialized] public int ObjectID = 0;
		public LightSpriteHandler lightSprite;


		[SyncVar(hook = nameof(SynchStrength))]
		[PlayModeOnly, NonSerialized] public float SynchroniseStrength = 0;

		[FormerlySerializedAs("SynchroniseStrength")]
		public float InitialStrength = 0;

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
			InitialStrength = SynchroniseStrength;
			Colour = InitialColour;

			ObjectID = this.GetInstanceID();


			lightSprite.SetColor(Colour);
			UpdateValues(SynchroniseStrength);
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
			Invalue = Mathf.Max(0, Invalue);
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

			if (Invalue < 0)
			if (Invalue < 0)
			{
				Invalue = 0;
			}

			OutPuttingRadiation = Invalue;
			float LightPower = OutPuttingRadiation / 24000;
			float LightSize = OutPuttingRadiation / 40000;
			if (LightPower > 1)
			{
				LightPower = 1;
			}
			lightSprite.transform.localScale = Vector3.one * (7 * LightSize);
			var Colour = lightSprite.GetColor();
			Colour.a = LightPower;
			lightSprite.SetColor(Colour);
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

			//Loggy.Log("RequestPulse!!" + Time.time);
		}
	}
}