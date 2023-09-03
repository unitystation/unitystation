using System;
using Logs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Mirror;

namespace Objects.Construction
{
	/// <summary>
	/// PM: A component for securing and unsecuring objects with a wrench. Meant to be generic.
	/// Based on Girder.cs. Comments mostly preserved.
	/// </summary>
	public class WrenchSecurable : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		private RegisterObject registerObject;
		private UniversalObjectPhysics objectBehaviour;
		private HandApply currentInteraction;
		private string objectName;

		/// <summary>
		/// Invoked after the anchored state is changed.
		/// </summary>
		[NonSerialized] public UnityEvent OnAnchoredChange = new UnityEvent();

		public bool IsAnchored => objectBehaviour != null && objectBehaviour.IsNotPushable;

		[SerializeField, FormerlySerializedAs("stateSecuredStatus")]
		[Tooltip("Whether the object will state if it is secured or unsecured upon examination.")]
		private bool isSecuredStateExaminable = true;

		[SerializeField, FormerlySerializedAs("RequireFloorPlatingExposed")]
		[Tooltip("Whether the object can be secured with floor tiles or the plating must be exposed.")]
		private bool isExposedFloorPlatingRequired = false;

		//The two float values below will likely be identical most of the time, but can't hurt to keep them separate just in case.
		[Tooltip("Time taken to secure this.")] [SerializeField]
		private float secondsToSecure = 0;

		[Tooltip("Time taken to unsecure this.")] [SerializeField]
		private float secondsToUnsecure = 0;

		[Tooltip("Whether the object sprite direction shouldn't be allowed to change, needs directional component")]
		public bool lockSpriteDirection = false;

		[HideInInspector] public bool blockAnchorChange;
		public string blockMessage;


		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			// Try get the best name for the object, else default to object's prefab name.
			if (TryGetComponent<ObjectAttributes>(out var attributes)
			    && string.IsNullOrWhiteSpace(attributes.InitialName) == false)
			{
				objectName = attributes.InitialName;
			}
			else
			{
				objectName = gameObject.ExpensiveName();
			}

			if (objectBehaviour == null)
			{
				Loggy.LogWarning($"{nameof(objectBehaviour)} was not found on {this}!", Category.Construction);
			}
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only care about interactions targeting us
			if (interaction.TargetObject != gameObject) return false;
			//only try to interact if the user has a wrench in their hand
			return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (blockAnchorChange)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, blockMessage);
				return;
			}

			currentInteraction = interaction;
			TryWrench();
		}

		public string Examine(Vector3 worldPos = default)
		{
			if (isSecuredStateExaminable)
			{
				return IsAnchored ? "It is anchored in place." : "It is currently not anchored.";
			}

			return default;
		}

		#endregion Interactions

		private void TryWrench()
		{
			if (IsAnchored)
			{
				Unanchor();
			}
			else
			{
				// Try anchor
				if (VerboseFloorExists() == false) return;
				if (isExposedFloorPlatingRequired && VerbosePlatingExposed() == false) return;
				if (ServerValidations.IsAnchorBlocked(currentInteraction)) return;

				Anchor();
			}
		}

		private bool VerboseFloorExists()
		{
			if (MatrixManager.IsEmptyAt(registerObject.WorldPositionServer, true, registerObject.Matrix.MatrixInfo) == false) return true;

			Chat.AddExamineMsg(currentInteraction.Performer, $"A floor must be present to secure the {objectName}!");
			return false;
		}

		private bool VerbosePlatingExposed()
		{
			if (!registerObject.TileChangeManager.MetaTileMap.HasTile(registerObject.LocalPositionServer,
				LayerType.Floors))
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
				secondsToSecure == 0
					? ""
					: $"{currentInteraction.Performer.ExpensiveName()} starts securing the {objectName}...",
				$"You secure the {objectName}.",
				$"{currentInteraction.Performer.ExpensiveName()} secures the {objectName}.",
				() => SetAnchored(true)
			);
		}

		private void Unanchor()
		{
			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, secondsToUnsecure,
				secondsToSecure == 0 ? "" : $"You start unsecuring the {objectName}...",
				secondsToSecure == 0
					? ""
					: $"{currentInteraction.Performer.ExpensiveName()} starts unsecuring the {objectName}...",
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

		public void ServerSetPushable(bool isPushable)
		{
			objectBehaviour.SetIsNotPushable(!isPushable);
		}
	}
}
