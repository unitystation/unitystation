using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using Systems.Interaction;
using Systems.ObjectConnection;
using Doors;


namespace Objects.Wallmounts
{
	/// <summary>
	/// Allows object to function as a door switch - opening / closing door when clicked.
	/// </summary>
	[ExecuteInEditMode]
	public class DoorSwitch : SubscriptionController, ICheckedInteractable<HandApply>, IMultitoolMasterable,
		IServerSpawn, ICheckedInteractable<AiActivate>
	{
		private SpriteRenderer spriteRenderer;
		public Sprite greenSprite;
		public Sprite offSprite;
		public Sprite redSprite;

		[Header("Access Restrictions for ID")] [Tooltip("Is this door restricted?")]
		public bool restricted;

		[Tooltip("Access level to limit door if above is set.")] [ShowIf(nameof(restricted))]
		public Access access;

		[SerializeField] [Tooltip("List of doors that this switch can control")]
		private List<DoorController> doorControllers = new List<DoorController>();

		private List<DoorMasterController> NewdoorControllers = new List<DoorMasterController>();

		private bool buttonCoolDown = false;
		private AccessRestrictions accessRestrictions;

		public void OnSpawnServer(SpawnInfo info)
		{
		}

		private void Awake()
		{
#if Unity_Editor
		noDoorsImg = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Textures/EditorAssets/noDoor.png");
#endif
		}

		private void Start()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
			gameObject.layer = LayerMask.NameToLayer("WallMounts");
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			accessRestrictions = gameObject.AddComponent<AccessRestrictions>();
			if (restricted)
			{
				accessRestrictions.restriction = access;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (buttonCoolDown)
				return;
			buttonCoolDown = true;
			StartCoroutine(CoolDown());

			if (accessRestrictions != null && restricted)
			{
				if (!accessRestrictions.CheckAccess(interaction.Performer))
				{
					RpcPlayButtonAnim(false);
					return;
				}
			}

			RunDoorController();
			RpcPlayButtonAnim(true);
		}

		private void RunDoorController()
		{
			if (doorControllers.Count == 0 && NewdoorControllers.Count == 0)
			{
				return;
			}

			foreach (var door in doorControllers)
			{
				// Door doesn't exist anymore - shuttle crash, admin smash, etc.
				if (door == null) continue;

				if (door.IsClosed)
				{
					door.TryOpen(null);
				}
				else
				{
					door.TryClose();
				}
			}

			foreach (var door in NewdoorControllers)
			{
				// Door doesn't exist anymore - shuttle crash, admin smash, etc.
				if (door == null) continue;

				if (door.IsClosed)
				{
					door.TryOpen(null);
				}
				else
				{
					door.TryClose();
				}
			}
		}

		//Stops spamming from players
		IEnumerator CoolDown()
		{
			yield return WaitFor.Seconds(1.2f);
			buttonCoolDown = false;
		}

		[ClientRpc]
		public void RpcPlayButtonAnim(bool status)
		{
			StartCoroutine(ButtonFlashAnim(status));
		}

		IEnumerator ButtonFlashAnim(bool status)
		{
			if (spriteRenderer == null)
			{
				spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			}

			for (int i = 0; i < 6; i++)
			{
				if (status)
				{
					if (spriteRenderer.sprite == greenSprite)
					{
						spriteRenderer.sprite = offSprite;
					}
					else
					{
						spriteRenderer.sprite = greenSprite;
					}

					yield return WaitFor.Seconds(0.2f);
				}
				else
				{
					if (spriteRenderer.sprite == redSprite)
					{
						spriteRenderer.sprite = offSprite;
					}
					else
					{
						spriteRenderer.sprite = redSprite;
					}

					yield return WaitFor.Seconds(0.1f);
				}
			}

			spriteRenderer.sprite = greenSprite;
		}

		#region Editor

		private void OnDrawGizmosSelected()
		{
			var sprite = GetComponentInChildren<SpriteRenderer>();
			if (sprite == null)
				return;

			//Highlighting all controlled doors with red lines and spheres
			Gizmos.color = new Color(1, 0.5f, 0, 1);
			for (int i = 0; i < doorControllers.Count; i++)
			{
				var doorController = doorControllers[i];
				if (doorController == null) continue;
				Gizmos.DrawLine(sprite.transform.position, doorController.transform.position);
				Gizmos.DrawSphere(doorController.transform.position, 0.25f);
			}

			for (int i = 0; i < NewdoorControllers.Count; i++)
			{
				var doorController = NewdoorControllers[i];
				if (doorController == null) continue;
				Gizmos.DrawLine(sprite.transform.position, doorController.transform.position);
				Gizmos.DrawSphere(doorController.transform.position, 0.25f);
			}
		}

		private void OnDrawGizmos()
		{
			if ((doorControllers.Count == 0 || doorControllers.Any(controller => controller == null)) ||
			    (NewdoorControllers.Count == 0 || NewdoorControllers.Any(controller => controller == null)))
			{
				Gizmos.DrawIcon(transform.position, "noDoor");
			}
		}

		public override IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
		{
			var approvedObjects = new List<GameObject>();

			foreach (var potentialObject in potentialObjects)
			{
				var doorController = potentialObject.GetComponent<DoorMasterController>();
				if (doorController == null)
				{
					var OlddoorController = potentialObject.GetComponent<DoorController>();
					if (OlddoorController == null )continue;
					AddDoorControllerFromScene(OlddoorController);
				}
				else
				{
					NewAddDoorControllerFromScene(doorController);
				}

				approvedObjects.Add(potentialObject);
			}

			return approvedObjects;
		}

		public void AddDoorControllerFromScene(DoorController doorController)
		{
			if (doorControllers.Contains(doorController))
			{
				doorControllers.Remove(doorController);
			}
			else
			{
				doorControllers.Add(doorController);
			}
		}

		public void NewAddDoorControllerFromScene(DoorMasterController doorController)
		{
			if (NewdoorControllers.Contains(doorController))
			{
				NewdoorControllers.Remove(doorController);
			}
			else
			{
				NewdoorControllers.Add(doorController);
			}
		}

		#endregion

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			RunDoorController();
			RpcPlayButtonAnim(true);
		}

		#endregion

		#region Multitool Interaction

		[SerializeField] private MultitoolConnectionType conType = MultitoolConnectionType.DoorButton;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion
	}
}