using UnityEngine;

namespace NPC
{
	/// <summary>
	/// AI brain for mice
	/// used to get hunted by Runtime and squeak
	/// </summary>
	public class MouseAI : GenericFriendlyAI
	{
		protected override void MonitorExtras()
		{
			//TODO eat cables if haven't eaten in a while

			timeWaiting += Time.deltaTime;
			if (timeWaiting < timeForNextRandomAction)
			{
				return;
			}
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(minTimeBetweenRandomActions, maxTimeBetweenRandomActions);

			DoRandomSqueek();
		}

		public override void OnPetted(GameObject performer)
		{
			Squeak();
			StartFleeing(performer, 3f);
		}

		protected override void OnFleeingStopped()
		{
			BeginExploring();
		}

		private void Squeak()
		{
			SoundManager.PlayNetworkedAtPos(
				"MouseSqueek",
				gameObject.transform.position,
				Random.Range(.6f, 1.2f));

			Chat.AddActionMsgToChat(
				gameObject,
				$"{mobNameCap} squeaks!",
				$"{mobNameCap} squeaks!");
		}

		private void DoRandomSqueek()
		{
			Squeak();
		}
	}
}