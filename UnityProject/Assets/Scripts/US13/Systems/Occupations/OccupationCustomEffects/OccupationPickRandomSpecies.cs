using System.Collections.Generic;
using US13.Player;
using US13.Systems.Lobby;
using US13.Systems.Occupations.OccupationCustomEffects.Interfaces;

namespace US13.Systems.Occupations.OccupationCustomEffects
{
	public class OccupationPickRandomSpecies : OccupationCustomEffectBase, IModifyCharacterSettings
	{
		public List<PlayerHealthData> ToChooseFrom = new List<PlayerHealthData>();

		public CharacterSheet ModifyingCharacterSheet(CharacterSheet characterSheet)
		{
			characterSheet = CharacterSheet.GenerateRandomCharacter(ToChooseFrom);
			return characterSheet;
		}
	}
}
