using Systems.Clearance;
using UnityEngine;

namespace Objects.Traps
{
	public interface IGenericTrigger
	{
		TriggerType TriggerType { get; }
		GameObject gameObject { get; }

		public void OnTrigger() { return; }
		public void OnTriggerWithClearance(IClearanceSource source) { return; }
		public void OnTriggerEnd();
	}
	public enum TriggerType
	{
		Active = 0, //This device will turn on when triggered and turn off when the trigger ends
		Toggle = 1, //This device will toggle its state whenever it is triggered
		HoldState = 2,	//Once triggered, this device will not turn off
	}
}
