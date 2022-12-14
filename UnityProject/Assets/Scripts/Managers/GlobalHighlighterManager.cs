using System;
using System.Collections;
using System.Linq;
using Core.Highlight;
using Shared.Managers;
using UnityEngine;

namespace Managers
{
	public class GlobalHighlighterManager : SingletonManager<GlobalHighlighterManager>
	{
		[SerializeField] private int maximumSearchIndexesPerFrame = 50;
		[SerializeField] private GameObject highlightObjectWorld;

		public static GameObject HighlightObject => Instance.highlightObjectWorld;

		public static void Highlight(string searchName)
		{
			Instance.StartCoroutine(Instance.Search(searchName.ToLower()));
		}

		private IEnumerator Search(string searchName)
		{
			var index = -1;
			foreach (var possibleTarget in FindObjectsOfType<GameObject>())
			{
				index++;
				if (index > maximumSearchIndexesPerFrame)
				{
					index = 0;
					yield return WaitFor.EndOfFrame;
				}
				if (possibleTarget.TryGetComponent<IHighlightable>(out var target) == false) continue;
				if (target.SearchableString().Any(foundName => string.Equals(foundName, searchName, StringComparison.CurrentCultureIgnoreCase)))
				{
					target.HighlightObject();
				}
			}
		}
	}
}
