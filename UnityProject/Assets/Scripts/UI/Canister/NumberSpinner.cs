using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Controls an entire number spinner - a display made up of DigitSpinners.
/// </summary>
public class NumberSpinner : MonoBehaviour
{
	public DigitSpinner Ones;
	public DigitSpinner Tens;
	public DigitSpinner Hundreds;
	public DigitSpinner Thousands;

	private int currentValue = 0;

	public void SpinUp()
	{
		Spin(true);
	}

	public void SpinDown()
	{
		Spin(false);
	}

	private void JumpTo(int newValue)
	{
		if (newValue > 9999 || newValue < 0)
		{
			Logger.LogErrorFormat("New value {0} is out of range, should be between 0 and 9999 inclusive",
				Category.UI, newValue);
		}
		Ones.JumpToDigit(newValue % 10);
		Tens.JumpToDigit(newValue / 10 % 10);
		Ones.JumpToDigit(newValue / 100 % 10);
		Ones.JumpToDigit(newValue / 1000 % 10);
	}

	/// <summary>
	/// Animate to the next digit.
	/// </summary>
	/// <param name="up"></param>
	private void Spin(bool up)
	{
		if (currentValue == 0 && !up || currentValue == 9999 && up)
		{
			Logger.LogErrorFormat("Current value is {0}, cannot spin in the specified direction as" +
			                      " it would take this spinner over our max of 9999 or under our min of 0", Category.UI, currentValue);
		}
		int prev = currentValue;
		currentValue = up ? currentValue + 1 : currentValue - 1;

		Ones.Spin(up);

		if (up)
		{
			if (prev % 10 == 9)
			{
				Tens.Spin(true);
			}
			if (prev % 100 == 99)
			{
				Hundreds.Spin(true);
			}
			if (prev % 1000 == 999)
			{
				Thousands.Spin(true);
			}
		}
		else
		{
			if (prev % 10 == 0)
			{
				Tens.Spin(false);
			}
			if (prev % 100 == 0)
			{
				Hundreds.Spin(false);
			}
			if (prev % 1000 == 0)
			{
				Thousands.Spin(false);
			}
		}
	}
}
