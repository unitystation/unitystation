using System.Collections;
using Player.Movement;
using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Dance")]
	public class Dance : EmoteSO
	{
		public override void Do(GameObject player)
		{
			if (allowEmoteWhileInCrit == false && CheckPlayerCritState(player) == false)
			{
				//Hacky way to run a coroutine inside an SO
				var something = player.GetComponent<PlayerScript>();
				something.StartCoroutine(PerformDance(player));
			}
			else
			{
				base.Do(player);
			}
		}

		private IEnumerator PerformDance(GameObject player)
		{
			var directional = player.transform.GetComponent<Rotatable>();
			var move = player.transform.GetComponent<MovementSynchronisation>();

			if (move.AllowInput && !move.IsBuckled)
			{
				Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewText}.");
				directional.FaceDirection(OrientationEnum.Up_By0);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(OrientationEnum.Left_By90);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(OrientationEnum.Right_By270);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(OrientationEnum.Down_By180);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(OrientationEnum.Up_By0);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(OrientationEnum.Left_By90);
				yield return WaitFor.Seconds(Random.Range(0.1f, 0.5f));
				directional.FaceDirection(OrientationEnum.Right_By270);
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
