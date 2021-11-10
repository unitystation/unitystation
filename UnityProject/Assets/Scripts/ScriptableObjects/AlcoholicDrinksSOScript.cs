using Systems.Chemistry;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "AlcoholicDrinksSOScript", menuName = "Singleton/AlcoholicDrinksSOScript")]
	public class AlcoholicDrinksSOScript : SingletonScriptableObject<AlcoholicDrinksSOScript>
	{
		public Reagent[]  AlcoholicReagents = new Reagent[0];
	}
}