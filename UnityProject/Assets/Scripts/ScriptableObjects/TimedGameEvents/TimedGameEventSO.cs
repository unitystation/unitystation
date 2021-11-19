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
		[NaughtyAttributes.ResizableTextArea] public string EventDesc;
		public Sprite EventIcon;
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

