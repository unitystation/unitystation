using System.Collections;
using Light2D;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Radiation
{
	public class RadiationProducer : NetworkBehaviour
	{
		public float OutPuttingRadiation = 0;
		public Color color = new Color(93f/255f, 202/255f, 49/255f, 0);
		private GameObject mLightRendererObject;
		private ObjectBehaviour objectBehaviour;
		private RegisterObject registerObject;
		public int ObjectID = 0;
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
			color = new Color(93f/255f, 202/255f, 49/255f, 0);

			objectBehaviour = this.GetComponent<ObjectBehaviour>();
			ObjectID = this.GetInstanceID();

			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, color, 7);
			mLightRendererObject.SetActive(true);

			lightSprite = mLightRendererObject.GetComponent<LightSprite>();
			lightSprite.Sprite = DotSprite;
			registerObject = this.GetComponent<RegisterObject>();
		}


		private void OnEnable()
		{
			UpdateManager.Add(RequestPulse, 5);
		}

		private void OnDisable()
		{
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

		public void Setlevel(float Invalue)
		{
			SynchStrength(SynchroniseStrength, Invalue);
		}

		private void UpdateValues(float Invalue)
		{
			OutPuttingRadiation = Invalue;
			float LightPower = OutPuttingRadiation / 24000;
			if (LightPower > 1)
			{
				mLightRendererObject.transform.localScale = Vector3.one * 7 *  LightPower ;
				LightPower = 1;
			}

			lightSprite.Color.a = LightPower;
		}

		void RequestPulse()
		{
			if (OutPuttingRadiation > 0.358f)
			{
				if (registerObject == null)
				{
					RadiationManager.Instance.RequestPulse(objectBehaviour.registerTile.Matrix,
						objectBehaviour.registerTile.LocalPosition, OutPuttingRadiation, ObjectID);
				}
				else
				{
					RadiationManager.Instance.RequestPulse(registerObject.Matrix,
						registerObject.LocalPosition, OutPuttingRadiation, ObjectID);
				}

			}

			UpdateValues(OutPuttingRadiation);

			//Logger.Log("RequestPulse!!" + Time.time);
		}
	}
}