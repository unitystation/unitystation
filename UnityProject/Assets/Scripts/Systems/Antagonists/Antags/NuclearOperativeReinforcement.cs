using UnityEngine;
using Systems.Teleport;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/NuclearOperativeReinforcement")]
	public class NuclearOperativeReinforcement : NuclearOperative
	{
		public override void AfterSpawn(ConnectedPlayer player)
		{
			UpdateChatMessage.Send(player.GameObject, ChatChannel.Syndicate, ChatModifier.None,
				$"We have intercepted the code for the nuclear weapon: <b>{AntagManager.SyndiNukeCode}</b>.");

			AntagManager.TryInstallPDAUplink(player, 0, true);
		}
	}
}
