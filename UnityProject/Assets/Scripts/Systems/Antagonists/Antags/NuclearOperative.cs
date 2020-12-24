using UnityEngine;
using Objects.Command;

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
			// add any NuclearOperative specific logic here

			//send the code:
			//Check to see if there is a nuke and communicate the nuke code:
			Nuke nuke = FindObjectOfType<Nuke>();
			if (nuke != null)
			{
				UpdateChatMessage.Send(player.GameObject, ChatChannel.Syndicate, ChatModifier.None,
					$"We have intercepted the code for the nuclear weapon: <b>{nuke.NukeCode}</b>.");
			}

			AntagManager.TryInstallPDAUplink(player, initialTC);
		}
	}
}
