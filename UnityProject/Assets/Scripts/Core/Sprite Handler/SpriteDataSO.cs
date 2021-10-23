using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using NaughtyAttributes;

[CreateAssetMenu(fileName = "SpriteData", menuName = "ScriptableObjects/SpriteData")]
public class SpriteDataSO : ScriptableObject
{
	public List<Variant> Variance = new List<Variant>();
	public bool IsPalette = false;
	public int setID = -1;

	public string DisplayName;

	[Serializable]
	public class Variant
	{
		public List<Frame> Frames = new List<Frame>();
	}


	[Serializable]
	public class Frame
	{
		public Sprite sprite;
		public float secondDelay;
	}

#if UNITY_EDITOR
	public void Awake()
	{
		{
			if (setID == -1)
			{
				if (SpriteCatalogue.Instance == null)
				{
					Resources.LoadAll<SpriteCatalogue>("ScriptableObjects/SOs singletons");
				}

				if (!SpriteCatalogue.Instance.Catalogue.Contains(this))
				{

					SpriteCatalogue.Instance.AddToCatalogue(this);
				}

				setID = SpriteCatalogue.Instance.Catalogue.IndexOf(this);
				Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(EditorSave(), this);
			}
		}
	}

	public void UpdateIDLocation()
	{
		if (setID == -1)
		{
			if (SpriteCatalogue.Instance == null)
			{
				Resources.LoadAll<SpriteCatalogue>("ScriptableObjectsSingletons");
			}

			if (!SpriteCatalogue.Instance.Catalogue.Contains(this))
			{

				SpriteCatalogue.Instance.AddToCatalogue(this);
			}

			setID = SpriteCatalogue.Instance.Catalogue.IndexOf(this);
			EditorUtility.SetDirty(this);
			EditorUtility.SetDirty( SpriteCatalogue.Instance);
		}
	}

	IEnumerator EditorSave()
	{
		yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(3);
		EditorUtility.SetDirty(this);
		EditorUtility.SetDirty( SpriteCatalogue.Instance);
		AssetDatabase.SaveAssets();
	}

	[Button()]
	public void ForceUpdateID()
	{
		setID = -1;
		UpdateIDLocation();
	}
#endif
}
