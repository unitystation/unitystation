using UnityEngine;
using Objects.Traps;
using System.Text;
using Mirror;
using UI.Systems.Tooltips.HoverTooltips;
using System.Collections.Generic;

namespace Objects.Logic
{
	public class LogicTimer : GenericTriggerOutput, IGenericTrigger, IHoverTooltip
	{
		private bool state = false;
		private bool output = false;

		[SerializeField] private SpriteHandler spriteHandler = null;
		[SerializeField, Tooltip("The time between rising edges of triggers")] private float timingDelay = 2;

		[field: SerializeField] public TriggerType TriggerType { get; protected set; }

		[SerializeField] private bool startEnabled = false;


		protected override void Awake()
		{
			SyncList();
			if (startEnabled) EnableTimer();
		}

		public void OnTrigger()
		{
			if (TriggerType == TriggerType.Toggle) ToggleState();
			else if (state == false) EnableTimer();
		}

		private void ToggleState()
		{
			if (state == true) DisableTimer();
			else EnableTimer();
		}

		public void OnTriggerEnd()
		{
			if (TriggerType != TriggerType.Active) return;
			DisableTimer();
		}
		
		private void EnableTimer()
		{
			state = true;
			UpdateManager.Add(ToggleOutput, timingDelay/2);
		}
		private void DisableTimer()
		{
			state = false;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ToggleOutput);
		}

		public void ToggleOutput()
		{
			if(output == false)
			{
				TriggerOutput();
				spriteHandler.SetSpriteVariant(1);
				output = true;
				return;
			}

			ReleaseOutput();
			spriteHandler.SetSpriteVariant(0);
			output = false;
		}

		public void OnDestroy()
		{
			if(state == true) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ToggleOutput);
		}

		#region Tooltips

		public string HoverTip()
		{
			StringBuilder sb = new StringBuilder();

			if (state == true) sb.AppendLine("The timer is currently active");
			else sb.AppendLine("The timer is currently inactive");

			return sb.ToString();
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return null;
		}
		#endregion
	}
}
