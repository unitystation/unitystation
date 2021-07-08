using UnityEngine;
using Objects.Command;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/NukeOps")]
	public class NukeOps : GameMode
	{
		public override bool IsPossible()
		{
			return base.IsPossible() && (FindObjectOfType<Nuke>() != null);
		}
	}
}
