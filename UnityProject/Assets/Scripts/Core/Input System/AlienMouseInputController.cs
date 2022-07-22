using System;
using Systems.Antagonists;
using UnityEngine.EventSystems;

public class AlienMouseInputController : MouseInputController
{
	private AlienPlayer alienPlayer;

	private void Awake()
	{
		alienPlayer = GetComponent<AlienPlayer>();
	}

	public override void CheckMouseInput()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			//Don't do any game world interactions if we are over the UI
			return;
		}

		if (UIManager.IsMouseInteractionDisabled)
		{
			//Still allow tooltips
			CheckHover();
			return;
		}

		if (CommonInput.GetMouseButtonDown(0))
		{

		}
		else
		{
			CheckHover();
		}
	}
}