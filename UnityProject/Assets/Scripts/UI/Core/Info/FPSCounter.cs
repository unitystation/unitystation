using UnityEngine;

public class FPSCounter : MonoBehaviour
{
	public int fps;
	public int lastFPS;

	public GUIStyle textStyle;
	private float timeA;

	// Use this for initialization
	private void Start()
	{
		timeA = Time.timeSinceLevelLoad;
		DontDestroyOnLoad(this);
	}

	private void Update()
	{
		if (Time.timeSinceLevelLoad - timeA <= 1)
		{
			fps++;
		}
		else
		{
			lastFPS = fps + 1;
			timeA = Time.timeSinceLevelLoad;
			fps = 0;
		}
	}

	private void OnGUI()
	{
		GUI.Label(new Rect(80, 5, 16, 16), "" + lastFPS, textStyle);
	}
}