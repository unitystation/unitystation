using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SpriteCatalogueSingleton", menuName = "Singleton/SpriteCatalogueSingleton")]
public class SpriteCatalogue : SingletonScriptableObject<SpriteCatalogue>
{
	public List<SpriteDataSO> Catalogue = new List<SpriteDataSO>();

	private static Dictionary<int,SpriteDataSO > resistantCatalogue = new Dictionary<int, SpriteDataSO>();

	public static Dictionary<int,SpriteDataSO> ResistantCatalogue
	{
		get
		{
			if (resistantCatalogue.Count == 0)
			{
				Instance.GenerateResistantCatalogue();
			}
			return resistantCatalogue;
		}
	}

	public void AddToCatalogue(SpriteDataSO spriteDataSO)
	{
		for (int i = 0; i < Catalogue.Count; i++)
		{
			if (Catalogue[i] == null)
			{
				Catalogue[i] = spriteDataSO;
				return;
			}
		}
		Catalogue.Add(spriteDataSO);
	}

	public void GenerateResistantCatalogue()
	{
		for (int i = 0; i < Catalogue.Count; i++)
		{
			var Cata = Catalogue[i];

			if (Cata != null)
			{
				Cata.SetID = i;
				resistantCatalogue[Cata.SetID] = Cata;
			}
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpriteCatalogue))]
public class SpriteCatalogueEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		SpriteCatalogue spriteCatalogue = (SpriteCatalogue)target;

		if (GUILayout.Button("Remove Null Items"))
		{
			RemoveNullItems(spriteCatalogue);
		}
	}

	private void RemoveNullItems(SpriteCatalogue spriteCatalogue)
	{
		spriteCatalogue.Catalogue.RemoveAll(item => item == null);
		EditorUtility.SetDirty(spriteCatalogue);
	}
}
#endif