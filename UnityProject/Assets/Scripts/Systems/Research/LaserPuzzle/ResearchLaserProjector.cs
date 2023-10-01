using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using Newtonsoft.Json;
using Systems.Electricity;
using Systems.Electricity.NodeModules;
using Systems.Research.Objects;
using UnityEngine;
using Weapons.Projectiles;
using Systems.Research.Data;
using UI.Core.Net;
using UI.Systems.Tooltips.HoverTooltips;

namespace Objects.Research
{
	public class ResearchLaserProjector : ResearchPointMachine, INodeControl, ICheckedInteractable<HandApply>, ICanOpenNetTab, IHoverTooltip
	{
		//TODO Go through and balance items , Done to a basic level

		//TODO Sprite collector
		//TODO https://www.youtube.com/watch?v=DwGcKFMxrmI

		private const float UPLOAD_EFFICIENCY = 0.75f; //If the player decides to upload research data as direct RP as opposed to technology, how efficient is this process?

		private LaserProjection LivingLine;

		private Rotatable rotatable;
		private ElectricalNodeControl electricalNodeControl;
		private SpriteHandler spriteHandler;
		private RegisterTile registerTile;
		private UniversalObjectPhysics objectPhysics;

		[Header("Electrical"), SerializeField]
		[Tooltip("The minimum voltage necessary to shoot")]
		private float minVoltage = 1500f;

		[SerializeField]
		[Tooltip("Whether this projector should start wrenched and welded")]
		private bool startSetUp;

		private float currentVoltage = 0;

		public Dictionary<Technology, float> GroupedData { get; private set; } = new Dictionary<Technology, float>();

		[Header("Misc"), SerializeField]
		private GameObject LaserProjectilePrefab;

		[SerializeField] private LaserProjection LaserProjectionprefab;

		[SerializeField] private float coolDownTimer = 5f;

		private float lastActivation = 0;

		public bool OnCoolDown => Time.time - lastActivation < coolDownTimer;

		public event Action UpdateGUI;

		public readonly List<string> OutputLogs = new List<string>();

		[field: SyncVar] public LaserProjectorState ProjectorState { get; private set; } = LaserProjectorState.Visual;
		[field: SyncVar] public bool IsVisualOn { get; private set; } = false;

		public bool hasPower { get; set; } = false;
		private bool isWrenched = false;
		private bool isWelded = false;

		[SyncVar(hook = nameof(UpdateLinesClient))]
		private string SynchronisedData;

		public void Awake()
		{
			rotatable = this.GetComponent<Rotatable>();
			electricalNodeControl = this.GetComponent<ElectricalNodeControl>();
			spriteHandler = this.GetComponentInChildren<SpriteHandler>();
			registerTile = this.GetComponent<RegisterTile>();
			objectPhysics = this.GetComponent<UniversalObjectPhysics>();

			if (startSetUp)
			{
				isWelded = true;
				isWrenched = true;
				rotatable.LockDirectionTo(true, rotatable.CurrentDirection);
				objectPhysics.SetIsNotPushable(true);
			}
		}

		[NaughtyAttributes.Button()]
		public void TriggerLaser()
		{
			if (hasPower == false) return;

			if (researchServer == null)
			{
				Loggy.LogError("Server Not Set");
				return;
			}

			IsVisualOn = true;

			if (LivingLine != null)
			{
				Destroy(LivingLine.gameObject);
			}
			gameObject.GetComponent<Collider2D>().enabled = false;
			LivingLine = Instantiate(LaserProjectionprefab, this.transform);
			LivingLine.Initialise(gameObject, rotatable.WorldDirection, this);
			gameObject.GetComponent<Collider2D>().enabled = true;
		}


		public void DisableLaser()
		{
			IsVisualOn = false;

			if (LivingLine != null)
			{
				LivingLine.CleanupAndDestroy();
			}
		}

		[NaughtyAttributes.Button()]
		public void FireLaser()
		{
			if (hasPower == false) return;

			var range = 30f;

			var Projectile = ProjectileManager.InstantiateAndShoot(LaserProjectilePrefab,
				rotatable.WorldDirection, gameObject, null, BodyPartType.None, range);

			var Data = Projectile.GetComponent<ContainsResearchData>();
			Data.Initialise(null, this);

			lastActivation = Time.time;

			UpdateGUI?.Invoke();
		}

		public void RegisterCollectorData(ResearchData data)
		{
			if (researchServer == null) return;

			if(GroupedData.ContainsKey(data.Technology) == true)
			{
				GroupedData[data.Technology] += data.ResearchPower;
			}
			else GroupedData.Add(data.Technology, data.ResearchPower);

			if (GroupedData[data.Technology] >= data.Technology.ResearchCosts)
			{
				if (researchServer.Techweb.ResearchedTech.Contains(data.Technology) == false)
				{
					OutputLogs.Add($">{data.Technology.DisplayName} Research Complete!");
					researchServer.Techweb.UnlockTechnology(data.Technology);
				}
				else
				{
					OutputLogs.Add($">{(int)GroupedData[data.Technology]} RP Uploaded!");
					AddResearchPoints(this, (int)GroupedData[data.Technology]);
				}

				GroupedData.Remove(data.Technology);
			}

			UpdateGUI?.Invoke();
		}

		public void TransferDataToRP()
		{
			float totalResearch = 0;

			foreach (var Technology in GroupedData)
			{
				totalResearch += Technology.Value;
			}

			GroupedData.Clear();

			OutputLogs.Add($">{(int)(totalResearch * UPLOAD_EFFICIENCY)} RP Uploaded!");
			AddResearchPoints(this, (int)(totalResearch * UPLOAD_EFFICIENCY));
		}

		public void PowerNetworkUpdate()
		{
			if (isWrenched == false || isWelded == false) hasPower = false;
			else
			{
				currentVoltage = electricalNodeControl.GetVoltage();
				hasPower = currentVoltage >= minVoltage;
			}
			spriteHandler.ChangeSprite(hasPower == true ? 0 : 1);
		}

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			if (!hasPower)
			{
				Chat.AddExamineMsgFromServer(playerObject, $"{gameObject.ExpensiveName()} is unpowered");
				return false;
			}
			return true;
		}

		public void UpdateState(LaserProjectorState state)
		{
			if (state != LaserProjectorState.Visual) DisableLaser();

			ProjectorState = state;
		}

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder)) return true;

			return hasPower;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				TryWrench(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder))
			{
				TryWeld(interaction);
			}

			PowerNetworkUpdate();
		}

		#endregion

		#region Weld

		private void TryWeld(HandApply interaction)
		{
			if (isWrenched == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The laser projector needs to be wrenched down first.");
				return;
			}

			if (interaction.HandObject.GetComponent<Welder>().IsOn == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You require a fueled and lit welder in order to proceed.");
				return;
			}

			if (isWelded)
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					"You begin to unweld the laser projector from the floor...",
					$"{interaction.Performer.ExpensiveName()} begins to unweld the laser projector from the floor...",
					"You unweld the laser projector from the floor.",
					$"{interaction.Performer.ExpensiveName()} unwelds the laser projector from the floor.",
					() =>
					{
						ElectricalManager.Instance.electricalSync.StructureChange = true;
						isWelded = false;
					});

				return;
			}

			ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
				"You begin to weld the laser projector to the floor...",
				$"{interaction.Performer.ExpensiveName()} begins to weld the laser projector to the floor...",
				"You weld the laser projector to the floor.",
				$"{interaction.Performer.ExpensiveName()} welds the laser projector to the floor.",
				() =>
				{
					ElectricalManager.Instance.electricalSync.StructureChange = true;
					isWelded = true;
				});

		}

		#endregion

		#region Wrench

		private void TryWrench(HandApply interaction)
		{
			if (isWrenched && isWelded)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The laser projector must to be unwelded first.");
			}
			else if (isWrenched)
			{
				//unwrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You begin to unwrench the laser projector from the floor...",
					$"{interaction.Performer.ExpensiveName()} begins to unwrench the laser projector from the floor...",
					"You unwrench the laser projector from the floor.",
					$"{interaction.Performer.ExpensiveName()} unwrenches the laser projector from the floor.",
					() =>
					{
						isWrenched = false;
						rotatable.LockDirectionTo(false, rotatable.CurrentDirection);
						objectPhysics.SetIsNotPushable(false);
					});
			}
			else
			{
				if (MatrixManager.IsSpaceAt(registerTile.WorldPositionServer, true, registerTile.Matrix.MatrixInfo))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "The laser projector must be on a floor or plating to be secured.");
					return;
				}

				//wrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You begin to wrench the laser projector to the floor...",
					$"{interaction.Performer.ExpensiveName()} begins to wrench the laser projector to the floor...",
					"You wrench the laser projector onto the floor.",
					$"{interaction.Performer.ExpensiveName()} wrenches the laser projector onto the floor.",
					() =>
					{
						isWrenched = true;
						rotatable.LockDirectionTo(true, rotatable.CurrentDirection);
						objectPhysics.SetIsNotPushable(true);
					});
			}
		}

		#endregion

		#region Tooltips

		public string HoverTip()
		{
			return null;
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		private bool LocalPlayerHasValidTool(ItemTrait requiredTool)
		{
			if (PlayerManager.LocalPlayerScript == null) return false;
			if (PlayerManager.LocalPlayerScript.DynamicItemStorage == null) return false;
			foreach (var slot in PlayerManager.LocalPlayerScript.DynamicItemStorage.GetHandSlots())
			{
				if (slot.IsEmpty) continue;
				if (slot.ItemAttributes.GetTraits().Contains(requiredTool)) return true;
			}

			return false;
		}

		public List<TextColor> InteractionsStrings()
		{
			List<TextColor> interactions = new List<TextColor>();

			ItemTrait traitToCheck;

			if (isWrenched == true && LocalPlayerHasValidTool(CommonTraits.Instance.Welder))
			{
				TextColor text = new TextColor
				{
					Text = "Left-Click with Welder: Weld/Unweld projector.",
					Color = IntentColors.Help
				};
				interactions.Add(text);
			}
			if(isWelded == false && LocalPlayerHasValidTool(CommonTraits.Instance.Wrench))
			{
				TextColor text = new TextColor
				{
					Text = "Left-Click with Wrench: Wrench/Unwrench projector.",
					Color = IntentColors.Help
				};
				interactions.Add(text);
			}

			return interactions;
		}

		#endregion

		#region Synchronisation

		public struct DataSynchronised
		{
			public string Origin;
			public string Target;
			public string Colour;
		}

		public void UpdateLinesClient(string Olddata, string Newdata)
		{
			SynchronisedData = Newdata;
			if (isServer) return;
			var Data = JsonConvert.DeserializeObject<List<DataSynchronised>>(Newdata);
			if (LivingLine != null)
			{
				Destroy(LivingLine.gameObject);
			}
			LivingLine = Instantiate(LaserProjectionprefab, this.transform);

			foreach (var line in Data)
			{
				LivingLine.ManualGenerateLine(line);
			}
		}

		public void SynchroniseLaser(List<LaserLine> LaserLines)
		{
			List<DataSynchronised> data = new List<DataSynchronised>();


			foreach (var LaserLine in LaserLines)
			{
				data.Add(new DataSynchronised()
				{
					Origin = LaserLine.VOrigin.ToSerialiseString(),
					Target = LaserLine.VTarget.ToSerialiseString(),
					Colour = LaserLine.Sprite.color.ToStringCompressed()
				});
			}
			SynchronisedData = JsonConvert.SerializeObject(data);
		}

		#endregion
	}

	public enum LaserProjectorState
	{
		Visual = 1,
		Live = 2,
	}
}
