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
		// for (int i = 0; i < Catalogue.Count; i++)
		// {
		// 	if (Catalogue[i] == null)
		// 	{
		// 		Catalogue[i] = spriteDataSO;
		// 		return;
		// 	}
		// }
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
