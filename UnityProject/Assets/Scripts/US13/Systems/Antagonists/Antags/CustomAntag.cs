using UnityEngine;
using US13.Player;

namespace US13.Systems.Antagonists.Antags
{
	public class CustomAntag : Antagonist
	{
		private string antagName;
		public new string AntagName => antagName;

		private void Init(string newAntagName)
		{
			antagName = newAntagName;
		}

		public static CustomAntag Create()
		{
			var toRet = ScriptableObject.CreateInstance<CustomAntag>();
			toRet.Init("CustomAntag");
			return toRet;
		}

		public override void AfterSpawn(Mind SpawnMind)
		{
			// Required for implementing
		}
	}
}