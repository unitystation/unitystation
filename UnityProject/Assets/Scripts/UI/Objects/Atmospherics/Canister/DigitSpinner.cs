using System;
using Logs;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core
{
	/// <summary>
	/// Animates a single spinning digit.
	/// </summary>
	public class DigitSpinner : MonoBehaviour
	{
		public float SpinSpeed = 200f;

		/// <summary>
		/// Invoked when a digit change has completed. Provides the new value
		/// </summary>
		[NonSerialized]
		public IntEvent OnDigitChangeComplete = new IntEvent();

		public Text center;
		public Text above;
		public Text below;

		private readonly Vector3 abovePosition = new Vector3(0, 48, 0);
		private readonly Vector3 belowPosition = new Vector3(0, -48, 0);
		/// <summary>
		/// Currently displayed digit - if animating, will be the digit we are animating FROM.
		/// </summary>
		public int CurrentDigit => currentDigit;
		private int currentDigit = 0;
		private int nextDigit = 0;

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		/// <summary>
		/// Instantly set the displayed text to the specified digit
		/// </summary>
		/// <param name="digit">Must be between 0 and 9 inclusive</param>
		public void JumpToDigit(int digit)
		{
			if (IsValidDigit(digit) == false) return;
			center.text = digit.ToString();
			above.text = Mod(digit + 1, 10).ToString();
			below.text = Mod(digit - 1, 10).ToString();
			currentDigit = digit;
			nextDigit = digit;
			center.transform.localPosition = Vector3.zero;
			above.transform.localPosition = abovePosition;
			below.transform.localPosition = belowPosition;
			OnDigitChangeComplete.Invoke(currentDigit);
		}

		// TODO: Surely System / Unity Math library can handle this?
		private int Mod(int a, int n)
		{
			int result = a % n;
			if ((result < 0 && n > 0) || (result > 0 && n < 0))
			{
				result += n;
			}
			return result;
		}

		/// <summary>
		/// Play spin animation to the next digit. If currently spinning, does nothing
		/// </summary>
		/// <param name="up"></param>
		public void Spin(bool up)
		{
			if (currentDigit != nextDigit) return;
			nextDigit = Mod(up ? currentDigit + 1 : currentDigit - 1, 10);
			// we move the center digit to the top or bottom then lerp it to the center
			if (up)
			{
				// next digit will lerp down from above
				center.transform.localPosition = abovePosition;
				center.text = nextDigit.ToString();
				// the current digit will lerp down as well
				below.transform.localPosition = Vector3.zero;
				below.text = currentDigit.ToString();
				above.transform.localPosition = abovePosition;
			}
			else
			{
				center.transform.localPosition = belowPosition;
				center.text = nextDigit.ToString();
				// current digit will lerp up
				above.transform.localPosition = Vector3.zero;
				above.text = currentDigit.ToString();
				below.transform.localPosition = belowPosition;
			}
		}

		private void UpdateMe()
		{
			// lerp if we need to
			if (currentDigit != nextDigit)
			{
				float step = SpinSpeed * Time.deltaTime;
				bool goUp = (nextDigit > currentDigit || nextDigit == 0 && currentDigit == 9) && !(nextDigit == 9 && currentDigit == 0);
				if (goUp)
				{
					// going up
					// next digit is higher, lerp digits down from above
					center.transform.localPosition =
						Vector3.MoveTowards(center.transform.localPosition, Vector3.zero, step);
					below.transform.localPosition =
						Vector3.MoveTowards(below.transform.localPosition, belowPosition, step);
				}
				else
				{
					// going down
					// lerp digits up from below
					center.transform.localPosition =
						Vector3.MoveTowards(center.transform.localPosition, Vector3.zero, step);
					above.transform.localPosition =
						Vector3.MoveTowards(above.transform.localPosition, abovePosition, step);
				}
				// check if we're done
				if (center.transform.localPosition.magnitude < .001f)
				{
					JumpToDigit(nextDigit);
				}
			}
		}

		private static bool IsValidDigit(int digit)
		{
			if (digit < 0 || digit > 9)
			{
				Loggy.LogErrorFormat("Specified digit {0} is out of range, must be value between 0 and 9 inclusive",
					Category.Atmos, digit);
				return false;
			}

			return true;
		}
	}
}
