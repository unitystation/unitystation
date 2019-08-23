using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_DevSelectVVTile : MonoBehaviour
{
	private enum State
	{
		INACTIVE,
		SELECTING,
	}

	public Text statusText;

	// objects selectable for cloning
	private LayerMask layerMask;

	//current state
	private State state;

	//object selected for cloning
	private EscapeKeyTarget escapeKeyTarget;

	private LightingSystem lightingSystem;

	void Awake()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
		ToState(State.SELECTING);
	}

	private void ToState(State newState)
	{
		if (newState == state)
		{
			return;
		}

		if (newState == State.SELECTING)
		{
			statusText.text = "Click to select object to view (ESC to Cancel)";
			UIManager.IsMouseInteractionDisabled = true;
			lightingSystem.enabled = false;

		}
		else if (newState == State.INACTIVE)
		{
			statusText.text = "Click to select object to view (ESC to Cancel)";
			UIManager.IsMouseInteractionDisabled = false;
			lightingSystem.enabled = true;
			gameObject.SetActive(false);
		}
		state = newState;
	}

	public void OnEscape()
	{
		if (state == State.SELECTING)
		{
			ToState(State.INACTIVE);
		}
	}

	private void OnDisable()
	{
		ToState(State.INACTIVE);
	}

	public void Open()
	{
		ToState(State.SELECTING);
	}

	private void Update()
	{
		if (state == State.SELECTING)
		{
			// ignore when we are over UI
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if (CommonInput.GetMouseButtonDown(0))
			{
				RequestToViewObjectsAtTile.Send(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition));
				OnEscape();
			}

		}
	}
}