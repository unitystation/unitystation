using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lobby
{
	public class CharacterView : MonoBehaviour
	{
		private Dictionary<string, CharacterSprites> sprites = new Dictionary<string, CharacterSprites>();

		public UnityEvent dirChangeEvent = new UnityEvent();
		public CharacterDir currentDir = CharacterDir.down;

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
			currentDir = (CharacterDir) nextDir;
			dirChangeEvent.Invoke();
		}
		public void RightRotate()
		{
			int nextDir = (int) currentDir - 1;
			if (nextDir < 0)
			{
				nextDir = 3;
			}
			currentDir = (CharacterDir) nextDir;
			dirChangeEvent.Invoke();
		}
	}

	public enum CharacterDir
	{
		down,
		left,
		up,
		right
	}
}