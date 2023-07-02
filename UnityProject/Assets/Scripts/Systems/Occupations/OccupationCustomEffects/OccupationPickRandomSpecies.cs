using System.Collections;
using System.Collections.Generic;
using Core.Utils;
using Systems.Character;
using UnityEngine;

public class OccupationPickRandomSpecies : OccupationCustomEffectBase, IModifyCharacterSettings
{
	public List<PlayerHealthData> ToChooseFrom = new List<PlayerHealthData>();

	public CharacterSheet ModifyingCharacterSheet(CharacterSheet CharacterSheet)
	{
		//GenerateRandomCharacter
		//randomise completely

		//only randomised species??
		CharacterSheet = CharacterSheet.GenerateRandomCharacter(ToChooseFrom);
		return CharacterSheet;
	}
}
