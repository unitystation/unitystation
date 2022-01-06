using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

