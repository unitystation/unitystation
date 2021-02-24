using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/NuclearOperative")]
	public class NuclearOperative : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 20;

		public override void AfterSpawn(ConnectedPlayer player)
		{
			UpdateChatMessage.Send(player.GameObject, ChatChannel.Syndicate, ChatModifier.None,
				$"We have intercepted the code for the nuclear weapon: <b>{AntagManager.SyndiNukeCode}</b>.");

			AntagManager.TryInstallPDAUplink(player, initialTC);
		}
	}
}
