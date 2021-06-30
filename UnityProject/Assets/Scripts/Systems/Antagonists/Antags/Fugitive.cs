using System;
using System.Threading.Tasks;
using Managers;
using Messages.Server;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Fugitive")]
	public class Fugitive: Antagonist
	{
		[SerializeField]
		[Tooltip("Min and max time in minutes it takes for Centcom to send a warning to the station about this fugitive." +
				"The real time will be a random time from the min to max values.")]
		private Vector2 timeToSendWarning = default;

		[SerializeField][TextArea(3, 10)]
		[Tooltip("The warning message that will be printed to command when Centcom alerts about this fugitive" +
				"{0} will be changed on runtime to the real name of the fugitive." +
				"{1} is just a random number to give some flavor.")]
		private string warnMessage =
				"We have reliable information to think there is a dangerous fugitive " +
				"from SuperJail in your station.\n" +
				"It is your duty to capture and bring them to Centcom or neutralize the threat " +
				"as soon as possible!\n\n" +
				"Prisoner number: {1}.\n" +
				"Prisoner name: {0}.\n" +
				"Category: EXTREMELY DANGEROUS.\n\n" +
				"<color=blue><size=32>New Station Objectives:</size></color>\n\n" +
				"<size=24>- Find the fugitive\n\n" +
				"- Arrest the fugitive and bring them to Centcom " +
				"when the escape shuttle is called\n\n" +
				"- If the fugitive is threatening the station production, neutralize immediately." +
				"</size>";

		public override void AfterSpawn(ConnectedPlayer player)
		{
			UpdateChatMessage.Send(player.GameObject, ChatChannel.Local, ChatModifier.Whisper,
				"I can't believe we managed to break out of a Nanotrasen superjail! Sadly though," +
				" our work is not done. The emergency teleport at the station logs everyone who uses it," +
				" and where they went. It won't be long until Centcom tracks where we've gone off to." +
				" I need to move in the shadows and keep out of sight," +
				" I'm not going back.");

			_ = StationWarning(player.Script.playerName);
		}

		private async Task StationWarning(string fugitiveName)
		{
			await Task.Delay(TimeSpan.FromMinutes(Random.Range(timeToSendWarning.x, timeToSendWarning.y)));
			int fugitiveNumber = Random.Range(1111, 9999);
			warnMessage = string.Format(warnMessage, fugitiveName, fugitiveNumber);

			GameManager.Instance.CentComm.MakeCommandReport(warnMessage);
		}
	}
}
