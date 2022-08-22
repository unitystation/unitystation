using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI.CharacterCreator
{
	public class CharacterView : MonoBehaviour
	{
		private Dictionary<string, CharacterSprites> sprites = new Dictionary<string, CharacterSprites>();

		public UnityEvent dirChangeEvent = new UnityEvent();
		public CharacterCustomization.CharacterDir currentDir = CharacterCustomization.CharacterDir.down;

		private void Awake()
		{
			foreach (CharacterSprites c in GetComponentsInChildren<CharacterSprites>())
			{
				sprites[c.name] = c;
			}
		}

		public void LeftRotate()
		{
			int nextDir = (int) currentDir + 1;
			if (nextDir > 3)
			{
				nextDir = 0;
			}
			currentDir = (CharacterCustomization.CharacterDir) nextDir;
			dirChangeEvent.Invoke();
		}

		public void RightRotate()
		{
			int nextDir = (int) currentDir - 1;
			if (nextDir < 0)
			{
				nextDir = 3;
			}
			currentDir = (CharacterCustomization.CharacterDir) nextDir;
			dirChangeEvent.Invoke();
		}
	}
}
