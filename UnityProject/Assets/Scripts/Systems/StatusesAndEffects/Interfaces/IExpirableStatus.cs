using System;

namespace Systems.StatusesAndEffects.Interfaces
{
	public interface IExpirableStatus
	{
		/// <summary>
		/// Invoke this method in your implementation of CheckExpiration when the status should expire.
		/// </summary>
		event Action<IExpirableStatus> Expired;

		/// <summary>
		/// How much time should this status last before expiration.
		/// </summary>
		float Duration { get; }

		/// <summary>
		/// When should this status expire? If OnAdded isn't overriden in your implementation, this will automatically
		/// be set to the current time + Duration.
		/// </summary>
		DateTime DeathTime { get; set; }

		void OnAdded()
		{
			DeathTime = DateTime.Now.AddSeconds(Duration);
			UpdateManager.Add(CheckExpiration, 1);
		}

		void OnRemoved()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckExpiration);
		}

		/// <summary>
		/// Implements the behaviour to check if this status has expired.
		/// If OnAdded and OnRemoved aren't overriden, this method will be automatically added to update manager
		/// You will have to add it otherwise.
		/// </summary>
		void CheckExpiration(); //it isn't possible to invoke the event from the interface, sadly.
	}
}