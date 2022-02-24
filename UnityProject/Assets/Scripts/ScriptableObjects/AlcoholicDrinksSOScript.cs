using System.Collections.Generic;
using Chemistry;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "AlcoholicDrinksSOScript", menuName = "Singleton/AlcoholicDrinksSOScript")]
	public class AlcoholicDrinksSOScript : SingletonScriptableObject<AlcoholicDrinksSOScript>
	{
		public List<Chemistry.Reagent>  AlcoholicReagents = new  List<Chemistry.Reagent>();

		private HashSet<Chemistry.Reagent> hashAlcoholicReagents;

		public HashSet<Chemistry.Reagent> HashAlcoholicReagents
		{
			get
			{
				if (hashAlcoholicReagents == null)
				{
					hashAlcoholicReagents = new HashSet<Reagent>(AlcoholicReagents);
				}
				return hashAlcoholicReagents;
			}
		}
	}
}