using System.Collections;
using System.Collections.Generic;
using MLAgents.CommunicatorObjects;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SpriteData", menuName = "ScriptableObjects/SpriteData")]
public class SpriteDataSO : ScriptableObject
{
	public List<Variant> Variance = new List<Variant>();
	public bool IsPalette = false;
	public int setID = -1;

	[System.Serializable]
	public class Variant
	{
		public List<Frame> Frames = new List<Frame>();
	}


	[System.Serializable]
	public class Frame
	{
		public Sprite sprite;
		public float secondDelay;
	}

#if UNITY_EDITOR
	public void Awake()
	{
		{
			//if (setID == -1)
			//{
				if (SpriteCatalogue.Instance == null)
				{
					Resources.LoadAll<SpriteCatalogue>("ScriptableObjects/SOs singletons");
				}

				if (!SpriteCatalogue.Instance.Catalogue.Contains(this))
				{
					SpriteCatalogue.Instance.Catalogue.Add(this);
				}

				setID = SpriteCatalogue.Instance.Catalogue.IndexOf(this);
				EditorUtility.SetDirty(this);

			//}
		}
	}
#endif
}