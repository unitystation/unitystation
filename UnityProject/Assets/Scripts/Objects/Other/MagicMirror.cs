using System.Collections;
using UnityEngine;
using Antagonists;
using AddressableReferences;

namespace Objects
{
	/// <summary>
	/// Allows the wizard to change their name and prints out a new identity paper.
	/// Eventually, should be able to change other things like race.
	/// </summary>
	public class MagicMirror : MonoBehaviour
	{
		private readonly float PRINTING_TIME = 2;

		[SerializeField] private AddressableAudioSource PrintSound = null;

		[SerializeField]
		private GameObject paperPrefab = default;

		private HasNetworkTab netTab;

		private Coroutine nameSettingRoutine;

		private void Awake()
		{
			netTab = GetComponent<HasNetworkTab>();
		}

		public void SetPlayerName(string newName)
		{
			ConnectedPlayer player = GetPlayer();

			if (newName.Length < 3)
			{
				Chat.AddExamineMsgFromServer(player.GameObject, "That doesn't seem to be an identifiable name. Too short, perhaps?");
				return;
			}

			this.RestartCoroutine(RunNameSetSequence(player, newName), ref nameSettingRoutine);
		}

		private IEnumerator RunNameSetSequence(ConnectedPlayer player, string newName)
		{
			SoundManager.PlayNetworkedAtPos(PrintSound, gameObject.RegisterTile().WorldPositionServer, sourceObj: gameObject);
			yield return WaitFor.Seconds(PRINTING_TIME);

			player.Script.SetPermanentName(newName);
			SpawnPaper(player);
		}

		private void SpawnPaper(ConnectedPlayer forPlayer)
		{
			GameObject paper = Spawn.ServerPrefab(paperPrefab, gameObject.RegisterTile().WorldPositionServer).GameObject;
			paper.GetComponent<Paper>().SetServerString(Wizard.GetIdentityPaperText(forPlayer));
		}

		private ConnectedPlayer GetPlayer()
		{
			return netTab.LastInteractedPlayer().Player();
		}
	}
}
