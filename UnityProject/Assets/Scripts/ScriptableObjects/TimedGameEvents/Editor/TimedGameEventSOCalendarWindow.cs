using System;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjects.TimedGameEvents.Editor
{
	public class TimedGameEventSOCalendarWindow : EditorWindow
	{
		private TimedGameEventSO timedGameEvent;
		private Vector2 scrollPos;

		// Dictionary to store temporary changes before applying to the actual object
		private SerializableDictionaryBase.Dictionary<Month, TimedGameEventSO.DayRange> monthDayRangesTemp = new();

		public static void ShowWindow(TimedGameEventSO timedGameEvent)
		{
			var window = GetWindow<TimedGameEventSOCalendarWindow>("Select Event Dates");
			window.timedGameEvent = timedGameEvent;

			// Create a temporary copy of the month-day ranges to work with in the window
			window.monthDayRangesTemp =
				new SerializableDictionaryBase.Dictionary<Month, TimedGameEventSO.DayRange>(timedGameEvent
					.MonthDayRanges);
		}

		private void OnGUI()
		{
			if (timedGameEvent == null) return;

			GUILayout.Label("Select Event Months and Day Ranges", EditorStyles.boldLabel);

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

			foreach (Month month in Enum.GetValues(typeof(Month)))
			{
				EditorGUILayout.BeginHorizontal();

				GUILayout.Label(month.ToString(), GUILayout.Width(100));

				// Check if the month is active in the event
				var isMonthActive = timedGameEvent.Months.Contains(month);
				var newIsMonthActive = EditorGUILayout.Toggle(isMonthActive);

				if (newIsMonthActive && !isMonthActive)
				{
					// Add the month and initialize the day range if the user selects it
					timedGameEvent.Months.Add(month);

					// Ensure that the dictionary has a DayRange for this month
					if (!monthDayRangesTemp.ContainsKey(month))
						monthDayRangesTemp[month] = new TimedGameEventSO.DayRange
							{ DayOfMonthStart = 1, DayOfMonthEnd = 31 };
				}
				else if (!newIsMonthActive && isMonthActive)
				{
					// Remove the month if the user deselects it
					timedGameEvent.Months.Remove(month);
					monthDayRangesTemp.Remove(month);
				}

				if (newIsMonthActive)
				{
					// Draw sliders for the day range only if the month is active
					if (!monthDayRangesTemp.ContainsKey(month))
						monthDayRangesTemp[month] = new TimedGameEventSO.DayRange
							{ DayOfMonthStart = 1, DayOfMonthEnd = 31 };

					var range = monthDayRangesTemp[month];
					range.DayOfMonthStart = EditorGUILayout.IntSlider("Start Day", range.DayOfMonthStart, 1, 31);
					range.DayOfMonthEnd = EditorGUILayout.IntSlider("End Day", range.DayOfMonthEnd, 1, 31);
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();

			// Buttons for applying or canceling changes
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Cancel")) Close();

			if (GUILayout.Button("Apply"))
			{
				ApplyChanges();
				Close();
			}

			EditorGUILayout.EndHorizontal();
		}

		private void ApplyChanges()
		{
			// Apply the temporary changes to the actual object
			timedGameEvent.MonthDayRanges =
				new SerializableDictionary<Month, TimedGameEventSO.DayRange>(monthDayRangesTemp);
			EditorUtility.SetDirty(timedGameEvent);
		}
	}
}