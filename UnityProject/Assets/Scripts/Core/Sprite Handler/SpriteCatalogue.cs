using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
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
		foreach (var Cata in Catalogue)
		{
			if (Cata != null)
			{
				if (resistantCatalogue.ContainsKey(Cata.setID))
				{
					Logger.LogError("OH GOD Duplicate ID on " + Cata.name + " and " + resistantCatalogue[Cata.setID].name);
				}

				resistantCatalogue[Cata.setID] = Cata;
			}
		}
	}
}
