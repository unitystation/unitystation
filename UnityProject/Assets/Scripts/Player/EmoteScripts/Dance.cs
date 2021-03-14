using System.Collections;
using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Dance")]
	public class Dance : EmoteSO
	{
		public override void Do(GameObject player)
		{
			//Hacky way to run a coroutine inside an SO
			var something = player.GetComponent<PlayerScript>();
			something.StartCoroutine(PerformDance(player));
		}

		private IEnumerator PerformDance(GameObject player)
		{
			var directional = player.transform.GetComponent<Directional>();
			var move = player.transform.GetComponent<PlayerMove>();

			if (move.allowInput && !move.IsBuckled)
			{
				base.Do(player);
				directional.FaceDirection(Orientation.Up);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(Orientation.Left);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(Orientation.Right);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(Orientation.Down);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(Orientation.Up);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(Orientation.Left);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(Orientation.Right);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
			}
			else
			{
				Chat.AddActionMsgToChat(player, $"{failText}", "");
			}

			yield break;
		}
	}
}
