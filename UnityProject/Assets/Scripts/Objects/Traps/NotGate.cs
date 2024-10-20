using UnityEngine;
using Objects.Traps;

namespace Objects.Logic
{
	[RequireComponent(typeof(LogicInput))]
	public class NotGate : GenericTriggerOutput
	{
		private LogicInput input;
		private bool state = false;

		[SerializeField] private SpriteHandler outputHandler = null;


		protected override void Awake()
		{
			input = GetComponent<LogicInput>();
			input.OnStateChangeEvent += OnInputUpdate;

			SyncList();
			OnInputUpdate(); //Affirm Inital State (Might start on)
		}

		private void OnInputUpdate()
		{
			bool oldState = state;

			state = !input.State;

			if (oldState != state) OnStateChange();
		}

		private void OnStateChange()
		{
			if (state == true) TriggerOutput();
			else ReleaseOutput();

			outputHandler.SetSpriteVariant(state ? 1 : 0);
		}
	}
}
