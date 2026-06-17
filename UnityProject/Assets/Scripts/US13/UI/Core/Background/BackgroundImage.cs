using System;
using UnityEngine;

namespace US13.UI.Core.Background
{
	[Serializable]
	public class BackgroundImage
	{
		[field: SerializeField] public Sprite Sprite { get; private set; }
		[field: SerializeField] public BackgroundFit Fit { get; private set; } = BackgroundFit.Cover;
	}
}
