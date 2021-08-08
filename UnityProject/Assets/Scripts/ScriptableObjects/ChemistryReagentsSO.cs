using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "ChemistryReagentsSO", menuName = "Singleton/ChemistryReagentsSO")]
	public class ChemistryReagentsSO : SingletonScriptableObject<ChemistryReagentsSO>
	{
		[SerializeField]
		private Chemistry.Reagent[]  allChemistryReagents = new Chemistry.Reagent[0];

		public Chemistry.Reagent[] AllChemistryReagents => allChemistryReagents;

		public void Awake()
		{
			for (int i = 0; i < allChemistryReagents.Length; i++)
			{
				allChemistryReagents[i].IndexInSingleton = i;
			}
		}
	}
}
