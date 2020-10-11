using UnityEngine;
using UnityEngine.Events;

///<Summary>
///Use this component to trigger a method with the enter key
///</Summary>

public class GUI_EnterMethod : GUI_Component
{
	public UnityEvent TriggerMethod;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			TriggerMethod.Invoke();
		}
	}
}
