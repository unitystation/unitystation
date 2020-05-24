using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Singleton. List of all spells mapped with corresponding spell data
/// </summary>
[CreateAssetMenu(fileName = "SpellListSingleton", menuName = "Singleton/SpellList")]
public class SpellList : SingletonScriptableObject<SpellList>
{
	public SpellMapping Spells = new SpellMapping();

	[SerializeField] private SpellData InvalidData = null;

	private List<MonoScript> spellKeys = new List<MonoScript>();

	private bool initialized = false;
	private void Initialize()
	{
		//setting transient indices for lookup
		spellKeys = Spells.Keys.ToList();
		foreach (var monoScript in spellKeys)
		{
			Spells[monoScript].index = spellKeys.IndexOf(monoScript);
		}

		initialized = true;
	}

	public static SpellData GetDataForSpell(Spell spell)
	{
		if (Instance == null)
		{
			Logger.LogError("SpellList instance not found!", Category.Spells);
			return null;
		}
		return Instance.InternalGetDataForSpell(spell);
	}
	private SpellData InternalGetDataForSpell(Spell spell)
	{
		if (!initialized)
		{
			Initialize();
		}

		foreach (var monoScript in spellKeys)
		{
			if (monoScript.GetClass() == spell.GetType())
			{
				return Spells[monoScript];
			}
		}
		Logger.LogErrorFormat("SpellList: no data mapped for spell {0}", Category.Spells, spell);
		return InvalidData;
	}
}