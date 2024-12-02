using UnityEngine;
using Objects.Traps;

namespace Objects.Logic
{
	public class LogicInput : MonoBehaviour, IGenericTrigger
	{
		public bool State
		{
			get
			{
				return state;
			}
			set
			{
				bool oldValue = state;
				state = value;
				if(state != oldValue) OnStateChangeEvent?.Invoke();
			}
		}
		private bool state;

		[field: SerializeField] public TriggerType TriggerType { get; protected set; }

		public delegate void OnStateChange();
		public OnStateChange OnStateChangeEvent;

		public void OnTrigger()
		{
			if (TriggerType == TriggerType.Toggle) State = !State;
			else if(State == false) State = true;
		}	

		public void OnTriggerEnd()
		{
			if (TriggerType != TriggerType.Active) return;
			State = false;
		}	
	}
}
