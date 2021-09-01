using Messages.Server;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/NuclearOperative")]
	public class NuclearOperative : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 25;

		public override void AfterSpawn(ConnectedPlayer player)
		{
			player.Job = JobType.SYNDICATE;
			UpdateChatMessage.Send(player.GameObject, ChatChannel.Syndicate, ChatModifier.None,
				$"We have intercepted the code for the nuclear weapon: <b>{AntagManager.SyndiNukeCode}</b>.", Loudness.LOUD);

			AntagManager.TryInstallPDAUplink(player, initialTC, true);
		}
	}
}
