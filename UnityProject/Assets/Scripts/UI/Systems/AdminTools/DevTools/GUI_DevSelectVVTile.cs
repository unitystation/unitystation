using DatabaseAPI;
using Messages.Client.VariableViewer;
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
	private bool cachedLightingState;

	public LightingSystem LightingSystem
	{
		get
		{
			if (lightingSystem == null)
			{
				lightingSystem = Camera.main.GetComponent<LightingSystem>();
			}

			return lightingSystem;
		}
		set
		{
			lightingSystem = value;
		}
	}

	private void Awake()
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
			cachedLightingState = LightingSystem.enabled;
			LightingSystem.enabled = false;
		}
		else if (newState == State.INACTIVE)
		{
			statusText.text = "Click to select object to view (ESC to Cancel)";
			UIManager.IsMouseInteractionDisabled = false;
			LightingSystem.enabled = cachedLightingState;
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
				RequestToViewObjectsAtTile.Send(MouseUtils.MouseToWorldPos());
				OnEscape();
			}

		}
	}
}
