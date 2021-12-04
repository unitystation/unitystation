using UnityEngine;
using UnityEngine.Events;

///<Summary>
///Use this component to trigger a method with the enter key
///</Summary>

public class GUI_EnterMethod : GUI_Component
{
	public UnityEvent TriggerMethod;

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	void UpdateMe()
	{
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			TriggerMethod.Invoke();
		}
	}
}
