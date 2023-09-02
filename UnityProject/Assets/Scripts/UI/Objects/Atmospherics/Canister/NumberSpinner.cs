using System;
using Logs;
using UnityEngine;

namespace UI.Core.NetUI
{
	/// <summary>
	/// Controls an entire number spinner - a display made up of DigitSpinners.
	/// </summary>
	public class NumberSpinner : NetUIStringElement
	{
		public int InitialValue = 9999;

		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		public DigitSpinner Ones;
		public DigitSpinner Tens;
		public DigitSpinner Hundreds;
		public DigitSpinner Thousands;
		// below 2 are optional - only used in the 6-digit version
		public DigitSpinner TenThousands;
		public DigitSpinner HundredThousands;

		public IntEvent OnValueChange = new IntEvent();

		/// <summary>
		/// Invoked when the value synced between client / server is updated.
		/// </summary>
		[NonSerialized]
		public IntEvent OnSyncedValueChanged = new IntEvent();

		/// <summary>
		/// Gets the value currently synced with the server / client
		/// </summary>
		public int SyncedValue => syncedValue;

		private bool init = false;
		private bool muteSounds = false;
		private static readonly float MIN_SECONDS_PER_TICK = .1f;
		private float tickCooldown = 0f;

		public override string Value {
			get { return syncedValue.ToString(); }
			protected set {
				int newVal = Convert.ToInt32(value);
				if (newVal == DisplayedValue) return;
				externalChange = true;
				Loggy.LogTraceFormat("NumberSpinner current value {0} New Value {1}", Category.Atmos, syncedValue, newVal);
				if (!IgnoreServerUpdates)
				{
					DisplaySpinTo(newVal);
				}
				syncedValue = newVal;
				OnSyncedValueChanged.Invoke(syncedValue);
				externalChange = false;
			}
		}

		public StringEvent ServerMethod;

		// latest value synced with server.
		private int syncedValue = 0;
		// current value being displayed on client side
		private int DisplayedValue => Ones.CurrentDigit + Tens.CurrentDigit * 10 + Hundreds.CurrentDigit * 100 +
									  Thousands.CurrentDigit * 1000
									  + (TenThousands != null ? TenThousands.CurrentDigit * 10000 : 0)
									  + (HundredThousands != null ? HundredThousands.CurrentDigit * 100000 : 0);

		private int MaxValue => HundredThousands != null ? 999999 : TenThousands != null ? 99999 : 9999;

		// targeted value being animated towards
		private int targetValue = 0;

		/// <summary>
		/// Turn this to true to ignore server updates, allowing for client prediction.
		/// </summary>
		public bool IgnoreServerUpdates;

		private void Awake()
		{
			Value = InitialValue.ToString();

			Ones.OnDigitChangeComplete.AddListener(OnOnesSpinComplete);
			// we will jump directly to the first value we get
			init = false;
			// check if we are the server version (will not play sounds if that's the case)
			muteSounds = GetComponentInParent<NetTab>().IsMasterTab;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (tickCooldown > 0)
			{
				tickCooldown = Math.Max(tickCooldown - Time.deltaTime, 0);
			}
		}

		/// <summary>
		/// Server side only. Set server target value to specified value
		/// </summary>
		/// <param name="newValue"></param>
		public void ServerSpinTo(int newValue)
		{
			if (newValue > MaxValue || newValue < 0)
			{
				Loggy.LogErrorFormat("New value {0} is out of range, should be between 0 and {1} inclusive",
					Category.Atmos, newValue, MaxValue);
			}
			//set the new value, to be propagated to clients.
			MasterSetValue(newValue.ToString());
		}

		public void DisplaySpinAdjust(int offset)
		{
			DisplaySpinTo(targetValue + offset);
		}

		/// <summary>
		/// Animate from the current value to the specified value, or jump to it if this is our initial value
		/// </summary>
		/// <param name="newValue"></param>
		public void DisplaySpinTo(int newValue)
		{
			if (newValue == DisplayedValue) return;

			if (newValue > MaxValue || newValue < 0)
			{
				Loggy.LogWarningFormat("New value {0} is out of range, should be between 0 and {1} inclusive",
					Category.Atmos, newValue, MaxValue);
			}

			newValue = Mathf.Clamp(newValue, 0, MaxValue);

			targetValue = newValue;
			if (muteSounds == false && tickCooldown <= 0)
			{
				_ = SoundManager.Play(CommonSounds.Instance.Tick); //0.15f, pan: -0.3f
				tickCooldown = MIN_SECONDS_PER_TICK;
			}

			//and just jump to the value because spinning looks bad when the spin rate is really high (which is needed
			//for internal pressure to update responsively)
			Ones.JumpToDigit(targetValue % 10);
			Tens.JumpToDigit(targetValue / 10 % 10);
			Hundreds.JumpToDigit(targetValue / 100 % 10);
			Thousands.JumpToDigit(targetValue / 1000 % 10);
			if (TenThousands != null)
			{
				TenThousands.JumpToDigit(targetValue / 10000 % 10);
			}

			if (HundredThousands != null)
			{
				HundredThousands.JumpToDigit(targetValue / 100000 % 10);
			}
			OnValueChange.Invoke(newValue);
			return;

			//NOTE: Previously tried to implement a spinning animation, but now am bypassing that stuff because it was
			//proving difficult to make it look good but also be responsive when pressure is changing rapidly.
			//Currently it just jumps directly to the number.
			//if (!init)
			//{
			//	//initial value - we just opened the view and are getting our initial value, so
			//	//jump directly to the new value
			//	Ones.JumpToDigit(targetValue % 10);
			//	Tens.JumpToDigit(targetValue / 10 % 10);
			//	Hundreds.JumpToDigit(targetValue / 100 % 10);
			//	Thousands.JumpToDigit(targetValue / 1000 % 10);
			//	if (TenThousands != null)
			//	{
			//		TenThousands.JumpToDigit(targetValue / 10000 % 10);
			//	}
			//
			//	if (HundredThousands != null)
			//	{
			//		HundredThousands.JumpToDigit(targetValue / 100000 % 10);
			//	}
			//	init = true;
			//}
			//else
			//{
			//	SpinOnceToTargetValue();
			//}
		}

		private void OnOnesSpinComplete(int newVal)
		{
			// continue spinning if we aren't at our target
			if (init && DisplayedValue != targetValue)
			{
				SpinOnceToTargetValue();
			}
		}

		/// <summary>
		/// Animate the displayed value one spin towards the target value.
		/// </summary>
		private void SpinOnceToTargetValue()
		{
			if (DisplayedValue == targetValue) return;
			bool up = targetValue > DisplayedValue;
			Ones.Spin(up);

			// TODO: consider datastructure, if possible, and avoid this branching
			if (up)
			{
				if (DisplayedValue % 10 == 9)
				{
					Tens.Spin(true);
				}
				if (DisplayedValue % 100 == 99)
				{
					Hundreds.Spin(true);
				}
				if (DisplayedValue % 1000 == 999)
				{
					Thousands.Spin(true);
				}
				if (DisplayedValue % 10000 == 9999)
				{
					TenThousands.Spin(true);
				}
				if (DisplayedValue % 100000 == 99999)
				{
					HundredThousands.Spin(true);
				}
			}
			else
			{
				if (DisplayedValue % 10 == 0)
				{
					Tens.Spin(false);
				}
				if (DisplayedValue % 100 == 0)
				{
					Hundreds.Spin(false);
				}
				if (DisplayedValue % 1000 == 0)
				{
					Thousands.Spin(false);
				}
				if (DisplayedValue % 10000 == 0)
				{
					TenThousands.Spin(false);
				}
				if (DisplayedValue % 100000 == 0)
				{
					HundredThousands.Spin(false);
				}
			}
		}

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(Value);
		}
	}
}
