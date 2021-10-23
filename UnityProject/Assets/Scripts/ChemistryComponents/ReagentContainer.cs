using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Items;
using Items.Others;
using Messages.Client.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Chemistry.Components
{
	/// <summary>
	/// Defines reagent container that can store reagent mix. All reagent mix logic done server side.
	/// Client can only interact with container by Interactions (Examine, HandApply, etc).
	/// </summary>
	public partial class ReagentContainer : MonoBehaviour, IServerSpawn, IRightClickable, ICheckedInteractable<ContextMenuApply>,
		IEnumerable<KeyValuePair<Reagent, float>>
	{
		[Header("Container Parameters")]

		[Tooltip("Max container capacity in units")]
		[SerializeField] private float maxCapacity = 100;
		public float MaxCapacity
		{
			get => maxCapacity;
			private set { maxCapacity = value; }
		}

		[Tooltip("Reactions list which can happen inside container. Use Default for generic containers")]
		[SerializeField] private ReactionSet reactionSet;
		public ReactionSet ReactionSet
		{
			get => reactionSet;
			private set { reactionSet = value; }
		}

		[Tooltip("If its unique container and can't be bothered to make SO")]
		public List<Reaction> AdditionalReactions = new List<Reaction>();

		[Tooltip("Can you empty out its contents?")]
		public bool canPourOut = true;

		[Tooltip("Initial mix of reagent inside container")]
		[FormerlySerializedAs("reagentMix")]
		[SerializeField] private ReagentMix initialReagentMix = new ReagentMix();
		[SerializeField]
		private bool destroyOnEmpty = default;

		private ItemAttributesV2 itemAttributes = default;
		private CustomNetTransform customNetTransform;
		private Integrity integrity;


		public bool ContentsSet = false;

		/// <summary>
		/// Invoked server side when regent container spills all of its contents
		/// </summary>
		[NonSerialized] public UnityEvent OnSpillAllContents = new UnityEvent();

		/// <summary>
		/// Invoked server side when the mix of reagents inside container changes
		/// </summary>
		[NonSerialized] public UnityEvent OnReagentMixChanged = new UnityEvent();


		private ReagentMix currentReagentMix;
		/// <summary>
		/// Server side only. Current reagent mix inside container.
		/// Invoke OnReagentMixChanged if you change anything in reagent mix
		/// </summary>
		public ReagentMix CurrentReagentMix
		{
			get
			{
				if (currentReagentMix == null)
				{
					if (initialReagentMix == null)
						return null;
					currentReagentMix = initialReagentMix.Clone();
				}
				return currentReagentMix;
			}
		}

		/// <summary>
		/// Returns reagent amount in container
		/// </summary>
		public float this[Reagent reagent] => CurrentReagentMix[reagent];

		public bool IsFull => ReagentMixTotal >= MaxCapacity;

		public bool IsEmpty => ReagentMixTotal <= 0f;

		/// <summary>
		/// Server side only. Current temperature of reagent mix
		/// </summary>
		public float Temperature
		{
			get => CurrentReagentMix.Temperature;
			set
			{
				CurrentReagentMix.Temperature = value;
				OnReagentMixChanged?.Invoke();
				ReagentsChanged();
			}
		}

		private string FancyContainerName
		{
			get
			{
				return itemAttributes ? itemAttributes.InitialName : gameObject.ExpensiveName();
			}
		}

		/// <summary>
		/// Server side only. Total reagent mix amount in units
		/// </summary>
		public float ReagentMixTotal
		{
			get
			{
				return CurrentReagentMix.Total;
			}
		}

		private void Awake()
		{
			// register spill on throw
			customNetTransform = GetComponent<CustomNetTransform>();
			if (customNetTransform)
			{
				customNetTransform.OnThrowEnd.AddListener(throwInfo =>
				{
					//check spill on throw
					if (!Validations.HasItemTrait(this.gameObject, CommonTraits.Instance.SpillOnThrow) || IsEmpty)
					{
						return;
					}

					SpillAll(thrown: true);
				});
			}

			// spill all content on destroy
			integrity = GetComponent<Integrity>();
			if (integrity)
			{
				integrity.OnWillDestroyServer.AddListener(info => SpillAll());
			}
			//OnReagentMixChanged.AddListener(ReagentsChanged);
		}

		public void ReagentsChanged()
		{
			if (ReactionSet != null)
			{
				ReactionSet.Apply(this, CurrentReagentMix,AdditionalReactions);
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			// reset all content on server respawn
			ResetContent();
		}

		/// <summary>
		/// Reset reagent mix to its initial state
		/// </summary>
		protected void ResetContent()
		{
			if (ContentsSet == false)
			{
				currentReagentMix = initialReagentMix.Clone();
			}

			ContentsSet = false;
			OnReagentMixChanged?.Invoke();
		}

		/// <summary>
		/// Server side only.
		/// Add reagent mix to container. May cause reaction inside container.
		/// Use MoveReagentsTo to transfer reagents from one container to another
		/// </summary>
		public TransferResult Add(ReagentMix addition)
		{
			// check whitelist reagents
			if (ReagentWhitelistOn)
			{
				lock (addition.reagents)
				{
					if (!addition.reagents.m_dict.All(r => reagentWhitelist.Contains(r.Key)))
					{
						return new TransferResult
						{
							Success = false,
							Message = "You can't transfer this into " + FancyContainerName
						};
					}
				}
			}

			// check if container is already full
			if (IsFull)
			{
				return new TransferResult
				{
					Success = false,
					Message = $"The {FancyContainerName} is full."
				};
			}

			// save total ammount before mixing
			var transferAmount = addition.Total;
			var beforeMixTotal = CurrentReagentMix.Total;
			var afterMixTotal = beforeMixTotal + transferAmount;

			// check if container can hold sum of mixes amount
			if (afterMixTotal > MaxCapacity)
			{
				// remove excess from addition mix
				// it will delete excess from world entirely
				transferAmount = MaxCapacity - beforeMixTotal;
				addition.Max(transferAmount, out _);
			}

			// add addition to reagent mix
			CurrentReagentMix.Add(addition);
			ReagentsChanged();

			// get mix total after all reactions,
			var afterReactionTotal = CurrentReagentMix.Total;

			var message = string.Empty;
			if (ReagentMixTotal > MaxCapacity)
			{
				//Reaction ends up in more reagents than container can hold
				CurrentReagentMix.Max(MaxCapacity, out _);
				message = $"Content starts overflowing out of {FancyContainerName}!";
			}

			OnReagentMixChanged?.Invoke();
			ReagentsChanged();
			return new TransferResult { Success = true, TransferAmount = transferAmount, Message = message };
		}

		/// <summary>
		/// Server side only. Subtract reagents from container
		/// Use MoveReagentsTo to transfer reagents from one container to another
		/// </summary>
		/// <param name="reagents"></param>
		public void Subtract(ReagentMix reagents)
		{
			CurrentReagentMix.Subtract(reagents);
			OnReagentMixChanged?.Invoke();
			ReagentsChanged();
		}

		/// <summary>
		/// Server side only. Subtract the specified reagent from the container.
		/// </summary>
		/// <param name="reagent"></param>
		/// <returns>Substracted amount</returns>
		public float Subtract(Reagent reagent, float subAmount)
		{
			float result = CurrentReagentMix.Subtract(reagent, subAmount);
			OnReagentMixChanged?.Invoke();
			ReagentsChanged();
			return result;
		}

		/// <summary>
		/// Server side only. Extracts reagents to be used outside ReagentContainer
		/// </summary>
		public ReagentMix TakeReagents(float amount)
		{
			var takeMix = CurrentReagentMix.Take(amount);
			OnReagentMixChanged?.Invoke();
			ReagentsChanged(); //Maybe not needed?
			return takeMix;
		}

		/// <summary>
		/// Server side only.
		/// Gets the amount of a particular reagent. Zero if it doesn't have this reagent.
		/// </summary>
		/// <param name="reagentName"></param>
		/// <returns></returns>
		public float AmountOfReagent(Reagent reagent)
		{
			return CurrentReagentMix[reagent];
		}

		/// <summary>
		/// Server side only.
		/// </summary>
		public void Divide(float Divider)
		{
			CurrentReagentMix.Divide(Divider);
			OnReagentMixChanged?.Invoke();
		}

		/// <summary>
		/// Server side only.
		/// </summary>
		public void Multiply(float multiplier)
		{
			CurrentReagentMix.Multiply(multiplier);
			OnReagentMixChanged?.Invoke();
			ReagentsChanged();
		}

		/// <summary>
		/// Server side only. Return color of the mix
		/// </summary>
		public Color GetMixColor()
		{
			return CurrentReagentMix.MixColor;
		}

		/// <summary>
		/// Server only. Returns [0,1] fill percents
		/// </summary>
		public float GetFillPercent()
		{
			if (IsEmpty)
				return 0f;

			return Mathf.Clamp01(CurrentReagentMix.Total / MaxCapacity);
		}

		/// <summary>
		/// Server only. Reagent with biggest amount in mix
		/// </summary>
		public Reagent MajorMixReagent => CurrentReagentMix.MajorMixReagent;

		public IEnumerator<KeyValuePair<Chemistry.Reagent, float>> GetEnumerator()
		{
			return CurrentReagentMix.reagents.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#region Spill
		private void SpillAll(bool thrown = false)
		{
			try
			{
				if (!IsEmpty)
				{
					var worldPos = customNetTransform.PushPull.AssumedWorldPositionServer();
					worldPos.z = 0;
					SpillAll(worldPos, thrown);
				}
			}
			catch (NullReferenceException exception)
			{
				Logger.LogError("Caught NRE in ReagentContainer SpillAll method: " + exception.Message, Category.Chemistry);
			}
		}

		/// <summary>
		/// Server side only.
		/// </summary>
		public void Spill(Vector3Int worldPos, float amount)
		{
			var spilledReagents = TakeReagents(amount);
			MatrixManager.ReagentReact(spilledReagents, worldPos);
		}

		private void SpillAll(Vector3Int worldPos, bool thrown = false)
		{
			NotifyPlayersOfSpill(worldPos);

			//todo: half decent regs spread
			var spilledReagents = TakeReagents(CurrentReagentMix.Total);
			MatrixManager.ReagentReact(spilledReagents, worldPos);

			OnSpillAllContents.Invoke();
		}

		private void NotifyPlayersOfSpill(Vector3Int worldPos)
		{
			var mobs = MatrixManager.GetAt<LivingHealthMasterBase>(worldPos, true);
			if (mobs.Count > 0)
			{
				foreach (var mob in mobs)
				{
					var mobObject = mob.gameObject;
					var mobName = mobObject.ExpensiveName();
					Chat.AddCombatMsgToChat(mobObject, mobName + " has been splashed with something!",
						mobName + " has been splashed with something!");
				}
			}
			else
			{
				Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()}'s contents spill all over the floor!", gameObject);
			}
		}

		#endregion

		public override string ToString()
		{
			return $"[{gameObject.ExpensiveName()}" +
				   $" |{ReagentMixTotal}/{MaxCapacity}|" +
				   $" ({string.Join(",", CurrentReagentMix)})" +
				   $" Mode: {transferMode}," +
				   $" TransferAmount: {TransferAmount}," +
				   $" {nameof(IsEmpty)}: {IsEmpty}," +
				   $" {nameof(IsFull)}: {IsFull}" +
				   "]";
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();
			result.AddElement("Contents", OnExamineContentsClicked);
			//Pour / add can only be done if in reach
			if (WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, "PourOut"), NetworkSide.Client) && canPourOut)
			{
				result.AddElement("PourOut", OnPourOutClicked);
			}
			return result;
		}

		private void OnExamineContentsClicked()
		{
			var menuApply = ContextMenuApply.ByLocalPlayer(gameObject, "Contents");
			RequestInteractMessage.Send(menuApply, this);
		}

		private void OnPourOutClicked()
		{
			var menuApply = ContextMenuApply.ByLocalPlayer(gameObject, "PourOut");
			RequestInteractMessage.Send(menuApply, this);
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			var eyeItem = interaction.Performer.GetComponent<Equipment>().GetClothingItem(NamedSlot.eyes).GameObjectReference;
			switch (interaction.RequestedOption)
			{
				case "Contents":
					{

						if (Validations.HasItemTrait(eyeItem, CommonTraits.Instance.ScienceScan))
						{
							eyeItem.GetComponent<ReagentScanner>().DoScan(interaction.Performer.gameObject, this.gameObject);
						}
						else
						{
							ExamineContents(interaction);
						}
						break;
					}
				case "PourOut":
					SpillAll();
					break;
			}
		}

		private void ExamineContents(ContextMenuApply interaction)
		{
			if (IsEmpty)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {gameObject.ExpensiveName()} is empty.");
				return;
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, this.Examine());
			}
		}

		/// <summary>
		/// Used for tests and debug only
		/// Create empty gameobject and add reagent containe with desired settings
		/// </summary>
		public static ReagentContainer Create(ReactionSet reactionSet, int maxCapacity)
		{
			GameObject obj = new GameObject();
			var container = obj.AddComponent<ReagentContainer>();

			container.ReactionSet = reactionSet;
			container.MaxCapacity = maxCapacity;

			return container;
		}
	}
}
