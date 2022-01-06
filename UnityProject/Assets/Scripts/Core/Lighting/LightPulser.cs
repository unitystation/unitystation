using Light2D;
using UnityEngine;

namespace Core.Lighting
{
	public class LightPulser : MonoBehaviour
	{
		[SerializeField]
		private float pulseSpeed = 0.5f; //here, a value of 0.5f would take 2 seconds and a value of 2f would take half a second

		[SerializeField]
		private float maxIntensity = 0.9f; // Max alpha is 1f, but lower so not blinding

		[SerializeField]
		private float minIntensity = 0.1f; // Min alpha is 0f, 0.1f so light doesnt go away completely

		private float targetIntensity = 1f;
		private float currentIntensity;

		private LightSprite lightSprite;

		private void Awake()
		{
			lightSprite = GetComponent<LightSprite>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, LightPulseTick);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, LightPulseTick);
		}

		private void LightPulseTick()
		{
			//Looping the light alpha to create a pulsing effect
			currentIntensity = Mathf.MoveTowards(lightSprite.Color.a, targetIntensity, Time.deltaTime * pulseSpeed);

			if(currentIntensity >= maxIntensity)
			{
				currentIntensity = maxIntensity;
				targetIntensity = minIntensity;
			}
			else if(currentIntensity <= minIntensity)
			{
				currentIntensity = minIntensity;
				targetIntensity = maxIntensity;
			}

			lightSprite.Color.a = currentIntensity;
		}

		public void SetMaxIntensity(float newMaxIntensity)
		{
			if (newMaxIntensity > 1)
			{
				newMaxIntensity = 1;
			}

			maxIntensity = newMaxIntensity;
		}

		public void SetMinIntensity(float newMinIntensity)
		{
			if (newMinIntensity < 0)
			{
				newMinIntensity = 0;
			}

			minIntensity = newMinIntensity;
		}

		public void SetPulseSpeed(float newSpeed)
		{
			pulseSpeed = newSpeed;
		}
	}
}