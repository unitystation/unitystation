using System.Collections;
using UnityEngine;

namespace ScriptableObjects.TimedGameEvents
{
	[CreateAssetMenu(fileName = "TimedGameEvent", menuName = "ScriptableObjects/Events/TimedGameEvent")]
	public class TimedGameEventSO : ScriptableObject
	{
		public Month Month;
		[Range(1,31)] public int DayOfMonthStart;
		[Range(1,31)] public int DayOfMonthEnd;

		public string EventName;
		[TextArea(10, 20)]
		public string EventDesc;

		public Sprite EventIcon;

		//Delete the object when the timed event is not happening
		public bool deleteWhenNotTime = true;

		/// <summary>
		/// If your timed event requires setup or anything when the round starts, use this.
		/// </summary>
		public virtual IEnumerator EventStart()
		{
			yield break;
		}
		/// <summary>
		/// If you want to do something after a round ends. (example: Add an entry to score machine)
		/// </summary>
		public virtual IEnumerator OnRoundEnd()
		{
			yield break;
		}

		/// <summary>
		/// Custom function to clean data when the round ends.
		/// </summary>
		public virtual void Clean()
		{
			//Scriptable objects carry over data from the previous round.
			//Implement your own custom logic to clean up your scriptable objects here.
			//This will be called when the round ends but you can call it whenever needed.
		}
	}

	public enum Month
	{
		January = 1,
		February = 2,
		March = 3,
		April = 4,
		May = 5,
		June = 6,
		July = 7,
		August = 8,
		September = 9,
		October = 10,
		November = 11,
		December = 12
	}
}

