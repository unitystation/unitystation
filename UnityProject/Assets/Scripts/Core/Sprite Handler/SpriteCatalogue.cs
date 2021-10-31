using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
[CreateAssetMenu(fileName = "SpriteCatalogueSingleton", menuName = "Singleton/SpriteCatalogueSingleton")]
public class SpriteCatalogue : SingletonScriptableObject<SpriteCatalogue>
{
	public List<SpriteDataSO> Catalogue = new List<SpriteDataSO>();

	public static Dictionary<int,SpriteDataSO > ResistantCatalogue = new Dictionary<int, SpriteDataSO>();

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
				if (ResistantCatalogue.ContainsKey(Cata.setID))
				{
					Logger.LogError("OH GOD Duplicate ID on " + Cata.name + " and " + ResistantCatalogue[Cata.setID].name);
				}

				ResistantCatalogue[Cata.setID] = Cata;
			}
		}
	}
}
