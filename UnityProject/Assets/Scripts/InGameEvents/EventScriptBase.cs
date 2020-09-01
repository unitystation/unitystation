using UnityEngine;

namespace InGameEvents
{
	/// <summary>
	/// Type of page for extra event parameters
	/// </summary>
	public enum ParametersPageType
	{
		None,
		Sickness,
	}

	/// <summary>
	/// The base script that events can be inherited from.
	///
	/// Also you'll need to add a condition for if the event is fake, as the bool in the base file here isnt used anywhere, to allow for the scripts to be more customisable.
	///
	///	The script must also be put on a gameobject in the EventManager prefab.
	///
	/// </summary>
	public class EventScriptBase : MonoBehaviour
	{
		/// <summary>
		/// Event name to be displayed in admin UI
		/// </summary>
		public string EventName = null;
		/// <summary>
		/// Delayed method start time
		/// </summary>
		public float StartTimer = 0f;
		/// <summary>
		/// Delayed method end time
		/// </summary>
		public float EndTimer = 0f;

		/// <summary>
		/// Event Type, for categories
		/// </summary>
		public InGameEventType EventType = InGameEventType.Fun;

		/// <summary>
		/// Chance to happen 1-100%
		/// </summary>
		public float ChanceToHappen = 100f;

		/// <summary>
		/// Minimum players needed to trigger event randomly
		/// </summary>
		public int MinPlayersToTrigger = 0;

		/// <summary>
		/// The type of extra parameters customization page
		/// </summary>
		public ParametersPageType parametersPageType = ParametersPageType.None;

		/// <summary>
		/// If the event is fake, you'll need to integrate into own script
		/// </summary>
		[HideInInspector]
		public bool FakeEvent = false;

		/// <summary>
		/// If the event is fake, you'll need to integrate into own script
		/// </summary>
		[HideInInspector]
		public bool AnnounceEvent = true;

		private void Start()
		{
			InGameEventsManager.Instance.AddEventToList(this, EventType);
		}

		private void OnDestroy()
		{
			InGameEventsManager.Instance.RemoveEventFromList(this, EventType);
			CancelInvoke();
		}

		public virtual void OnEventStart()
		{
			Invoke(nameof(OnEventStartTimed), StartTimer);
		}

		public virtual void OnEventStart(string serializedEventParameters = null)
		{
			OnEventStart();
		}

		public virtual void OnEventStartTimed()
		{
			OnEventEnd();
		}

		public virtual void OnEventEnd()
		{
			Invoke(nameof(OnEventEndTimed), EndTimer);
		}

		public virtual void OnEventEndTimed()
		{

		}

		public void TriggerEvent(string serializedEventParameters = null)
		{
			OnEventStart(serializedEventParameters);
		}
	}

	public enum InGameEventType
	{
		Random,
		Fun,
		Special,
		Antagonist,
		Debug
	}
}