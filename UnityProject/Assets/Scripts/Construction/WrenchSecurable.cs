using System;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using WebSocketSharp;

/// <summary>
/// PM: A component for securing and unsecuring objects with a wrench. Meant to be generic.
/// Based on Girder.cs. Comments mostly preserved.
/// </summary>
public class WrenchSecurable : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
{
	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	private HandApply currentInteraction;
	private string objectName;

	/// <summary>
	/// Invoked after the anchored state is changed.
	/// </summary>
	[NonSerialized]
	public UnityEvent OnAnchoredChange = new UnityEvent();
	public bool IsAnchored => !objectBehaviour.IsPushable;

	[SerializeField]
	[Tooltip("Whether the object will state if it is secured or unsecured upon examination.")]
	bool stateSecuredStatus = true;

	[SerializeField]
	[Tooltip("Whether the object can be secured with floor tiles or the plating must be exposed.")]
	bool RequireFloorPlatingExposed = false;

	//The two float values below will likely be identical most of the time, but can't hurt to keep them separate just in case.
	[Tooltip("Time taken to secure this.")]
	[SerializeField]
	private float secondsToSecure = 0;
	
	[Tooltip("Time taken to unsecure this.")]
	[SerializeField]
	private float secondsToUnsecure = 0;

	private void Start()
	{
		registerObject = GetComponent<RegisterObject>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

		// Try get the best name for the object, else default to object's prefab name.
		if (TryGetComponent(out ObjectAttributes attributes) && !attributes.InitialName.IsNullOrEmpty())
		{
			objectName = attributes.InitialName;
		}
		else objectName = gameObject.ExpensiveName();
	}

	#region Interactions

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		currentInteraction = interaction;
		TryWrench();
	}

	public string Examine(Vector3 worldPos = default)
	{
		if (stateSecuredStatus) return IsAnchored ? "It is anchored in place.": "It is currently not anchored.";
		return default;
	}

	#endregion Interactions

	private void TryWrench()
	{
		if (IsAnchored) Unanchor();
		else
		{
			// Try anchor
			if (!VerboseFloorExists()) return;
			if (RequireFloorPlatingExposed && !VerbosePlatingExposed()) return;
			if (ServerValidations.IsAnchorBlocked(currentInteraction)) return;

			Anchor();
		}
	}

	private bool VerboseFloorExists()
	{
		if (!MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true)) return true;

		Chat.AddExamineMsg(currentInteraction.Performer, $"A floor must be present to secure the {objectName}!");
		return false;
	}

	private bool VerbosePlatingExposed()
	{
		if (!registerObject.TileChangeManager.MetaTileMap.HasTile(registerObject.LocalPositionServer, LayerType.Floors, true))
		{
			return true;
		}

		Chat.AddExamineMsg(
				currentInteraction.Performer,
				$"The floor plating must be exposed before you can secure the {objectName} to the floor!");
		return false;
	}

	private void Anchor()
	{
		ToolUtils.ServerUseToolWithActionMessages(currentInteraction, secondsToSecure,
				secondsToSecure == 0 ? "" : $"You start securing the {objectName}...",
				secondsToSecure == 0 ? "" : $"{currentInteraction.Performer.ExpensiveName()} starts securing the {objectName}...",
				$"You secure the {objectName}.",
				$"{currentInteraction.Performer.ExpensiveName()} secures the {objectName}.",
				() => SetAnchored(true)
		);
	}

	private void Unanchor()
	{
		ToolUtils.ServerUseToolWithActionMessages(currentInteraction, secondsToUnsecure,
				secondsToSecure == 0 ? "" : $"You start unsecuring the {objectName}...",
				secondsToSecure == 0 ? "" : $"{currentInteraction.Performer.ExpensiveName()} starts unsecuring the {objectName}...",
				$"You unsecure the {objectName}.",
				$"{currentInteraction.Performer.ExpensiveName()} unsecures the {objectName}.",
				() => SetAnchored(false)
		);
	}

	private void SetAnchored(bool isAnchored)
	{
		objectBehaviour.ServerSetAnchored(isAnchored, currentInteraction.Performer);
		OnAnchoredChange?.Invoke();
	}
}
