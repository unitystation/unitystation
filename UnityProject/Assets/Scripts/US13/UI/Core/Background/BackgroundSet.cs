using System.Collections.Generic;
using UnityEngine;

namespace US13.UI.Core.Background
{
	/// <summary>
	/// A curated collection of background images used by the animated backgrounds on UI menus.
	/// </summary>
	[CreateAssetMenu(fileName = "BackgroundSet", menuName = "ScriptableObjects/UI/Background Set")]
	public class BackgroundSet : ScriptableObject
	{
		[SerializeField] private List<BackgroundImage> backgrounds = new List<BackgroundImage>();

		public IReadOnlyList<BackgroundImage> Backgrounds => backgrounds;

		public int Count => backgrounds.Count;

		public BackgroundImage GetAt(int index)
		{
			if (backgrounds.Count == 0) return null;
			index = Mathf.Clamp(index, 0, backgrounds.Count - 1);
			return backgrounds[index];
		}

		public BackgroundImage GetRandom()
		{
			if (backgrounds.Count == 0) return null;
			return backgrounds[Random.Range(0, backgrounds.Count)];
		}
	}
}
