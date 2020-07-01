using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
[CreateAssetMenu(fileName = "AlcoholicDrinksSOScript", menuName = "Singleton/AlcoholicDrinksSOScript")]
public class AlcoholicDrinksSOScript : SingletonScriptableObject<AlcoholicDrinksSOScript>
{
	public Chemistry.Reagent[]  AlcoholicReagents = new Chemistry.Reagent[0];
}