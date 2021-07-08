using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
[CreateAssetMenu(fileName = "SpriteCatalogueSingleton", menuName = "Singleton/SpriteCatalogueSingleton")]
public class SpriteCatalogue : SingletonScriptableObject<SpriteCatalogue>
{
	public List<SpriteDataSO> Catalogue = new List<SpriteDataSO>();

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
}
