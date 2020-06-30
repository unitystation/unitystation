using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component used to send radio messages from its parent object. Only functions on the server, so make sure to call it from there!
/// </summary>
public class RadioMessager : MonoBehaviour
{
	/// <summary>
	/// The frequency you wish to send the signal out with.
	/// </summary>
	[SerializeField]
	[Tooltip("The frequency that the signal should be sent at. Uses MHz.")]
	private float frequency = 200;
	[SerializeField]
	[Tooltip("The minium frequency range that the signal should be sent at. Uses MHz.")]
	private float frequencyMin = 0;
	[SerializeField]
	[Tooltip("The maximum frequency range the signal should be sent at. Uses MHz.")]
	private float frequencyMax = 0;

	[SerializeField]
	private bool useFrequencyRange = false;

	/// <summary>
	/// The amplitude of the signal to be sent out. This determines how far the signal travels.
	/// </summary>
	[SerializeField]
	[Tooltip("The amplitude of the signal. Used to determine effective range. Amplitude is the 'power' of the signal. Uses watts.")]
	private float amplitude = 0;

	[SerializeField]
	[Tooltip("The instensity of the signal. The higher the intensity, the greater the range. Uses watts/m^2.")]
	private float intensity = 5;

	/// <summary>
	/// The message to be sent over the signal. Can become garbled if the receiver is too far away.
	/// </summary>
	[SerializeField]
	[Tooltip("The message being sent in the signal. Can be null.")]
	private string message = null;

	public bool IsOn = true;

	private RegisterTile registerTile;
	public RegisterTile RegisterTitle => registerTile;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public virtual void SendSignal()
	{
		if (useFrequencyRange)
		{
			RadioSignal signal = new RadioSignal(frequencyMin, frequencyMax, amplitude, intensity, message);
			RadioManager.Instance.SendRadioSignal(signal, this);
		}
		else
		{
			RadioSignal signal = new RadioSignal(frequency, amplitude, intensity, message);
			RadioManager.Instance.SendRadioSignal(signal, this);
		}
	}

}
