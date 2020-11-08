using UnityEngine;
using DigitalRuby.LightningBolt;
using Light2D;

namespace Core.Lighting
{
	/// <summary>
	/// Controls the glow for the electrical arc effect. Intended for the ElectricalArcEffect prefab.
	/// </summary>
	[RequireComponent(typeof(LineRenderer))]
	public class ElectricalArcGlow : MonoBehaviour
	{
		[Tooltip("The line renderer with which positions will be used. Set this to the main line renderer.")]
		[SerializeField]
		private LineRenderer sourceLineRenderer = default;

		[Tooltip("How many glow sprites should exist per unit (tile). " +
				"Less artifacting at small densities, but resolution may suffer with large chaos.")]
		[SerializeField, Range(0.25f, 4)]
		private float spriteDensity = 1;

		[SerializeField]
		private Transform ambientTransform = default;
		[SerializeField]
		private Transform startTransform = default;
		[SerializeField]
		private Transform endTransform = default;

		private LineRenderer glowLineRenderer;

		private void Awake()
		{
			glowLineRenderer = GetComponent<LineRenderer>();

			GetComponentInParent<LightningBoltScript>().LightningTriggered += RunUpdate;
		}

		/// <summary>
		/// Sets the intensity for the glow line renderer, the hemispherical end glows and ambient glow.
		/// </summary>
		/// <param name="intensity">An intensity of 1 is no change, 2 - twice as intense, 0, off.</param>
		public void SetIntensity(float intensity)
		{
			// Glow line renderer
			Gradient glowLineGradient = glowLineRenderer.colorGradient;
			GradientAlphaKey[] glowLineAlphaKeys = glowLineGradient.alphaKeys;
			for (int i = 0; i < glowLineAlphaKeys.Length; i++)
			{
				glowLineAlphaKeys[i].alpha *= intensity;
			}
			glowLineGradient.alphaKeys = glowLineAlphaKeys;
			glowLineRenderer.colorGradient = glowLineGradient;

			// Hemisphere end glows
			LightSprite startLightSprite = startTransform.GetComponent<LightSprite>();
			LightSprite endLightSprite = endTransform.GetComponent<LightSprite>();
			Color endColor = endLightSprite.Color;
			endColor.a *= intensity;
			startLightSprite.Color = endColor;
			endLightSprite.Color = endColor;

			// Ambient glow
			LightSprite ambientLightSprite = ambientTransform.GetComponent<LightSprite>();
			Color ambientColor = ambientLightSprite.Color;
			ambientColor.a *= intensity;
			ambientLightSprite.Color = ambientColor;	
		}

		private void RunUpdate()
		{
			Vector3[] positions = new Vector3[sourceLineRenderer.positionCount];
			sourceLineRenderer.GetPositions(positions);

			// We get an approximate copy of the original line, with less segments.
			// This reduces a lot of visual artifacting that occured when having so many light sprites in the same place.
			Vector3[] glowPositions = ApproximateLine(positions);

			glowLineRenderer.positionCount = glowPositions.Length;
			glowLineRenderer.SetPositions(glowPositions);

			// The line glow only casts light perpendicular to the line,
			// so we have hemispherical light to make the ends of the line look nice.
			UpdateEndGlow(glowPositions);

			UpdateAmbientGlow();
		}

		private Vector3[] ApproximateLine(Vector3[] positions)
		{
			float distance = Vector3.Distance(positions[0], positions[positions.Length - 1]);

			int glowPointCount = ((int)Mathf.Round(spriteDensity * distance)) + 1;
			Vector3[] glowPositions = new Vector3[glowPointCount];

			// First and last positions will always be the same.
			glowPositions[0] = positions[0];
			glowPositions[glowPositions.Length - 1] = positions[positions.Length - 1];

			for (int i = 1; i < glowPositions.Length - 1; i++)
			{
				glowPositions[i] = positions[(int)Mathf.Round(i * (positions.Length / glowPositions.Length))];
			}

			return glowPositions;
		}

		private void UpdateEndGlow(Vector3[] positions)
		{
			// Update positions
			startTransform.position = positions[0];
			endTransform.position = positions[positions.Length - 1];

			// Update scale
			var glowWidth = glowLineRenderer.widthMultiplier;
			startTransform.localScale.Scale(new Vector3(1.25f * glowWidth, 1.25f * glowWidth, 1));
			endTransform.localScale.Scale(new Vector3(1.25f * glowWidth, 1.25f * glowWidth, 1));

			// Update angle of hemispherical sprite
			Vector3 startNextPos = positions[1];
			Vector3 endNextPos = positions[positions.Length - 2];
			if (positions.Length > 2) // Third position in from the end seems to give better results.
			{
				startNextPos = positions[2];
				endNextPos = positions[positions.Length - 3];
			}

			startTransform.right = positions[0] - startNextPos;
			endTransform.right = positions[positions.Length - 1] - endNextPos;
		}

		private void UpdateAmbientGlow()
		{
			// Position
			ambientTransform.position = Vector3.Lerp(startTransform.position, endTransform.position, 0.5f);

			// Scale
			float arcDistance = Vector3.Distance(startTransform.position, endTransform.position);
			ambientTransform.localScale = new Vector3(arcDistance + 2, glowLineRenderer.widthMultiplier + 1, 1);

			// Rotation
			ambientTransform.right = startTransform.position - endTransform.position;
		}
	}
}
