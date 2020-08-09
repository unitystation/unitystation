using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "AlcoholicDrinksSOScript", menuName = "Singleton/AlcoholicDrinksSOScript")]
	public class AlcoholicDrinksSOScript : SingletonScriptableObject<AlcoholicDrinksSOScript>
	{
		public Chemistry.Reagent[]  AlcoholicReagents = new Chemistry.Reagent[0];
	}
}