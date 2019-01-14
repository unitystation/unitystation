using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour
{
	private static DebugConsole debugConsole;
	public static DebugConsole Instance
	{
		get
		{
			if (debugConsole == null)
			{
				debugConsole = FindObjectOfType<DebugConsole>();
			}
			return debugConsole;
		}
	}

	protected static string DebugLog { get; private set; }
	protected static string LastLog { get; private set; }

	public Text displayText;
	public Text averageFPSText;
	public Text minimumFPSText;
	public GameObject consoleObject;

	bool isOpened = false;
	private List<float> fps = new List<float>();

	public static void AmendLog(string msg)
	{
		if (RconManager.Instance != null)
		{
			RconManager.AddLog(msg);
		}

		DebugLog += msg + "\n";
		LastLog = msg;
		if (DebugLog.Length > 10000)
		{
			DebugLog = DebugLog.Substring(9000);
		}

		//if it is null it means the object is still disabled and is about be enabled
		if (Instance != null)
		{
			Instance.RefreshLogDisplay();
		}
	}

	void Start()
	{
		Instance.consoleObject.SetActive(false);
		Instance.isOpened = false;
		StartCoroutine(UpdateFPSDisplay());
	}

	void OnEnable()
	{
		Application.logMessageReceived += LogCallback;
	}

	//Called when there is an exception
	void LogCallback(string condition, string stackTrace, LogType type)
	{
		if (type == LogType.Warning)
		{
			return;
		}

		if (type == LogType.Exception)
		{
			AmendLog(condition + " " + stackTrace);
		}
		else
		{
			AmendLog(condition + " " + type);
		}
	}

	void OnDisable()
	{
		Application.logMessageReceived -= LogCallback;
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F5))
		{
			ToggleConsole();
		}
		fps.Add(1f / Time.deltaTime);

	}

	void ToggleConsole()
	{
		isOpened = !isOpened;
		consoleObject.SetActive(isOpened);
	}

	void RefreshLogDisplay()
	{
		Instance.displayText.text = DebugLog;
	}

	private IEnumerator UpdateFPSDisplay()
	{
		while (true)
		{
			if (fps.Count <= 0)yield return null;

			float avgFPS = Mathf.Round(fps.Average());
			float minFPS = Mathf.Round(fps.Min());
			fps.Clear();

			averageFPSText.text = "Avg: " + Mathf.Round(avgFPS);
			averageFPSText.color = FPSColor(avgFPS);
			minimumFPSText.text = "Min: " + Mathf.Round(minFPS);
			minimumFPSText.color = FPSColor(minFPS);

			yield return new WaitForSeconds(.25f);
		}
	}

	private Color FPSColor(float fps)
	{
		var col =
			fps < 25 ? Color.red :
			fps < 55 ? Color.yellow :
			Color.green;
		return col /= 2;
	}
}