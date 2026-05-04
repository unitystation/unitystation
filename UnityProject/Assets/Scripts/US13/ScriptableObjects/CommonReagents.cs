using Chemistry;
using UnityEngine;

namespace US13.ScriptableObjects
{
	[CreateAssetMenu(fileName = "CommonReagents", menuName = "Singleton/CommonReagents")]
	public class CommonReagents : SingletonScriptableObject<CommonReagents>
	{
		public Reagent SmokePowder;
		public Reagent Fluorosurfactant;
		public Reagent Water;
		public Reagent Blood;
		public Reagent SpaceCleaner;
		public Reagent SpaceLube;
		public Reagent TableSalt;
	}
}
