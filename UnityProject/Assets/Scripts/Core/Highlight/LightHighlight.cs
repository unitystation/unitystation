using System;
using Light2D;
using Objects.Lighting;
using UnityEngine;

namespace Core.Highlight
{
	public class LightHighlight : MonoBehaviour
	{
		private LightSprite source;

		private void Awake()
		{
			source = GetComponent<LightSprite>();
		}

		private void Update()
		{
			var newColor = source.Color;
			newColor.a = Mathf.Lerp(source.Color.a, 0, .55f);
			source.Color = newColor;
			if(source.Color.a <= 0) Destroy(gameObject);
		}
	}
}