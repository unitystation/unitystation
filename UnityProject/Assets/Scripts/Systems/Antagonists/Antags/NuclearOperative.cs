using Messages.Server;
using Player.Language;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/NuclearOperative")]
	public class NuclearOperative : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 25;

		[SerializeField]
		private GameObject ImplantExplosive;

		[SerializeField]
		private LanguageSO codeSpeak;

		public override void AfterSpawn(Mind player)
		{
			UpdateChatMessage.Send(player.gameObject, ChatChannel.Syndicate, ChatModifier.None,
				$"We have intercepted the code for the nuclear weapon: <b>{AntagManager.SyndiNukeCode}</b>.", Loudness.LOUD);

			AntagManager.TryInstallPDAUplink(player, initialTC, true);

			GameObject implant = Spawn.ServerPrefab(ImplantExplosive, player.gameObject.AssumedWorldPosServer()).GameObject;

			if(player.body.playerHealth.brain == null)
			{
				Debug.LogError(player.CurrentCharacterSettings.Name + " has no brain to reference!");
				return;
			}

			player.body.playerHealth.brain.RelatedPart.ContainedIn.OrganStorage.ServerTryAdd(implant); //Step by step: Get's the players brain as we always have a reference to the brain, gets where the brain is (i.e head) and then puts the implant in there.

			player.body.MobLanguages.LearnLanguage(codeSpeak, true);
		}
	}
}
