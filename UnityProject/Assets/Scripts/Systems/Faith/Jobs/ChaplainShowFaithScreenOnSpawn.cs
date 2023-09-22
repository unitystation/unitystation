using Systems.Character;

namespace Systems.Faith.Jobs
{
	public class ChaplainShowFaithScreenOnSpawn : OccupationCustomEffectBase, IModifyCharacterSettings
	{
		//(Max): I couldn't find a better way to handle this so this stays like this i guess *shrug*
		public CharacterSheet ModifyingCharacterSheet(CharacterSheet CharacterSheet)
		{
			UIManager.Instance.ChaplainFirstTimeSelectScreen.gameObject.SetActive(true);
			return CharacterSheet;
		}
	}
}