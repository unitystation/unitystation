using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Serverside radio management tool. Clients cannot send radio messages! If you wish to send a radio message, call it on the server!
/// </summary>
public class RadioManager : SingletonManager<RadioManager>
{
	private RadioMessager LastRadioMessager = null;

	private List<RadioReceiver> receivers = new List<RadioReceiver>();

	public void RegisterReceiver(RadioReceiver rec) => receivers.Add(rec);

	public void RemoveReceiver(RadioReceiver rec) => receivers.Remove(rec);

	/// <summary>
	/// Check if a receiver can receive a signal from a messager. Probably a better way to do this.
	/// </summary>
	/// <param name="rec"></param>
	/// <param name="signal"></param>
	/// <param name="originator"></param>
	/// <returns></returns>

	//TODO
	//Degrade the message in a sent signal if it's below the sensitivity of the receiver (up to a certain range).
	//i.e. add corrupted text and stuff

	private bool CanReceiverReceiverSignal(RadioReceiver rec, RadioSignal signal, RadioMessager originator)
	{
		if (rec ==null || signal == null || originator == null) return false;

		//Prevent signallers spamming in a loop.
		if (originator == LastRadioMessager) return false;

		LastRadioMessager = originator;

		//Prevent a receiver picking up a signal if it was the one that sent it.
		if (rec.gameObject.Equals(originator.gameObject)) return false;

		if (signal.BroadRange)
		{
			if (rec.UseFrequencyRange)
			{
				if (signal.FrequencyMin >= rec.MinFrequency || signal.FrequencyMax <= rec.MaxFrequency)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.Intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}
			}
			else
			{
				if (signal.FrequencyMin < rec.Frequency && signal.FrequencyMax > rec.Frequency)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.Intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}

			}
		}
		else
		{
			if (rec.UseFrequencyRange)
			{
				if (signal.Frequency >= rec.MinFrequency && signal.Frequency <= rec.MaxFrequency)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.Intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}
			}
			else
			{
				//Have to use an epsilon check here, since floats are imperfect.
				if (Mathf.Abs(signal.Frequency - rec.Frequency) < 0.01)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.Intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	public void SendRadioSignal(RadioSignal signal, RadioMessager originator)
	{
		foreach (RadioReceiver rec in receivers)
		{
			if (CanReceiverReceiverSignal(rec, signal, originator))
			{
				rec.OnReceiveSignal(signal);
			}
		}
	}

	public void SendRadioSignalFromLocation(RadioSignal signal, Vector2 origin)	{ }
}

/// <summary>
/// These are the signals that will be sent out with the radio manager. They have a specified frequency, amplitude, intensity and message.
/// Frequencies use MHz, amplitude uses watts, intensity uses watts/m^2.
/// </summary>
public class RadioSignal
{
	public float Frequency { get; private set; }
	public float FrequencyMin { get; private set; }
	public float FrequencyMax { get; private set; }
	public float Amplitude { get; private set; }
	public float Intensity { get; private set; }
	public bool BroadRange { get; private set; }
	public string Message { get; private set; }

	//Message is null by default. For signals that only care about I/O.
	public RadioSignal(float frequency, float amplitude, float intensity, string message = null)
	{
		this.Frequency = frequency;
		this.Amplitude = amplitude;
		this.Intensity = intensity;
		this.BroadRange = false;
		this.Message = message;
	}

	public RadioSignal(float frequencyMin, float frequencyMax, float amplitude, float intensity, string message = null)
	{
		this.FrequencyMin = frequencyMin;
		this.FrequencyMax = frequencyMax;
		this.Amplitude = amplitude;
		this.Intensity = intensity;
		this.BroadRange = true;
		this.Message = message;
	}
}

