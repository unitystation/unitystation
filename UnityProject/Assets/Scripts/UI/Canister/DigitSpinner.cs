using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates a single spinning digit.
/// </summary>
public class DigitSpinner : MonoBehaviour
{
	[Tooltip("How many seconds to spin to the next number.")]
	public float SecondsPerSpin = .10f;

	public Text center;
	public Text above;
	public Text below;

	private Vector3 abovePosition;
	private Vector3 belowPosition;
	private int currentDigit = 0;
	private int nextDigit = 0;
	private float speed;

	private void Start()
	{
		//store these so we know where numbers should scroll from
		abovePosition = above.transform.localPosition;
		belowPosition = below.transform.localPosition;
	}

	/// <summary>
	/// Instantly set the displayed text to the specified digit
	/// </summary>
	/// <param name="digit">Must be between 0 and 9 inclusive</param>
	public void JumpToDigit(int digit)
	{
		if (!IsValidDigit(digit)) return;
		center.text = digit.ToString();
		above.text = (digit + 1 % 10).ToString();
		below.text = (digit - 1 % 10).ToString();
		currentDigit = digit;
		nextDigit = digit;
		center.transform.localPosition = Vector3.zero;
		above.transform.localPosition = abovePosition;
		below.transform.localPosition = belowPosition;
	}

	/// <summary>
	/// Play spin animation to the next digit. If currently spinning, jumps to the next digit.
	/// </summary>
	/// <param name="up"></param>
	public void Spin(bool up)
	{
		//jump to next digit if we are currently spinning
		currentDigit = nextDigit;
		nextDigit = (up ? currentDigit + 1 : currentDigit - 1) % 10;
		//we move the center digit to the top or bottom then lerp it to the center
		if (up)
		{
			center.transform.localPosition = abovePosition;
			center.text = nextDigit.ToString();
			above.transform.localPosition = Vector3.zero;
			above.text = currentDigit.ToString();
			below.transform.localPosition = belowPosition;
		}
		else
		{
			center.transform.localPosition = belowPosition;
			center.text = nextDigit.ToString();
			below.transform.localPosition = Vector3.zero;
			below.text = currentDigit.ToString();
			above.transform.localPosition = abovePosition;
		}

		//we recalculate this on each spin rather than on init so it can be played with in editor
		//distance from top / bottom should be the same
		speed = abovePosition.magnitude / SecondsPerSpin;
	}

	private void Update()
	{
		//lerp if we need to
		if (currentDigit != nextDigit)
		{
			float step = speed * Time.deltaTime;
			if (nextDigit > currentDigit || nextDigit == 0 && currentDigit == 9)
			{
				//moving up
				center.transform.localPosition =
					Vector3.MoveTowards(center.transform.localPosition, Vector3.zero, step);
				above.transform.localPosition =
					Vector3.MoveTowards(above.transform.localPosition, abovePosition, step);
			}
			if (nextDigit < currentDigit || currentDigit == 0 && nextDigit == 9)
			{
				//moving down
				center.transform.localPosition =
					Vector3.MoveTowards(center.transform.localPosition, Vector3.zero, step);
				below.transform.localPosition =
					Vector3.MoveTowards(below.transform.localPosition, belowPosition, step);
			}
			//check if we're done
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
			Logger.LogErrorFormat("Specified digit {0} is out of range, must be value between 0 and 9 inclusive",
				Category.UI, digit);
			return false;
		}

		return true;
	}
}
