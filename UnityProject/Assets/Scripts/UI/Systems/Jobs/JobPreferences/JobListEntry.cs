using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace UI
{
	/// <summary>
	/// The job entry script which allows setting the entry text and image.
	/// Also displays job info when hovering the mouse over it.
	/// </summary>
	public class JobListEntry : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("The job TMP label component")]
		private TMP_Text jobName = null;

		[SerializeField]
		[Tooltip("The job image component")]
		private Image jobImage = null;

		[SerializeField]
		[Tooltip("The priority dropdown component for this job")]
		private TMP_Dropdown dropdown = null;

		[SerializeField]
		[Tooltip("The main job preferences window")]
		private GUI_JobPreferences jobPreferences = null;

		[SerializeField]
		[Tooltip("The job info window")]
		private GUI_JobInfo jobInfo = null;

		[SerializeField]
		[Tooltip("The EventTrigger Component")]
		private EventTrigger eventTrigger = null;
		private Occupation occupation;

		/// <summary>
		/// Sets all fields for this entry from an occupation
		/// </summary>
		/// <param name="newOccupation"></param>
		public void Setup(Occupation newOccupation)
		{
			occupation = newOccupation;
			jobName.text = newOccupation.DisplayName;
			jobImage.sprite = newOccupation.PreviewSprite;

			// Job window listener
			// Adds an OnPointerEnter event which updates the jobInfo window with this entry's occupation info
			EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
			entry.callback.AddListener((eventData) => { jobInfo.Job = occupation; });
			eventTrigger.triggers.Add(entry);
		}

		/// <summary>
		/// Sets the priority for this job entry
		/// </summary>
		/// <param name="priority"></param>
		public void SetPriority(Priority priority)
		{
			dropdown.value = (int)priority;
		}

		/// <summary>
		/// Sends the priority update to the main job preferences script for processing
		/// </summary>
		/// <param name="value">The updated priority value as an int</param>
		public void OnValueChanged(int value)
		{
			Priority priority = (Priority)value;
			jobPreferences.OnPriorityChange(occupation.JobType, priority, this);
		}
	}
}
