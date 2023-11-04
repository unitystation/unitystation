using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items;
using Items.Others;
using Logs;
using Messages.Client.Interaction;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Chemistry.Components
{
	/// <summary>
	/// Defines reagent container that can store reagent mix. All reagent mix logic done server side.
	/// Client can only interact with container by Interactions (Examine, HandApply, etc).
	/// </summary>
	public partial class ReagentContainer : MonoBehaviour, IServerSpawn, IRightClickable,
		ICheckedInteractable<ContextMenuApply>,
		IEnumerable<KeyValuePair<Reagent, float>>, IServerDespawn
	{
		[Flags]
		private enum ShowMenuOptions
		{
			None = 0,
			ShowContents = 1 << 0,
			PourOut = 1 << 1,
			All = ~None
		}

		[Header("Container Parameters")] [Tooltip("Max container capacity in units")] [SerializeField]
		private float maxCapacity = 100;

		public float MaxCapacity
		{
			get => maxCapacity;
			private set { maxCapacity = value; }
		}

#if UNITY_EDITOR
		public Reagent DEBUGReagent;
		public float DEBUGAmount;

		[NaughtyAttributes.Button]
		public void ADDDEBUGReagent()
		{
			// add addition to reagent mix
			CurrentReagentMix.Add(DEBUGReagent, DEBUGAmount);

			ReagentsChanged(true);
		}

#endif


		//How much room is there left in the container
		public float SpareCapacity => maxCapacity - ReagentMixTotal;

		[Tooltip("Reactions list which can happen inside container. Use Default for generic containers")]
		[SerializeField]
		private ReactionSet reactionSet;

		public ReactionSet ReactionSet
		{
			get => reactionSet;
			private set { reactionSet = value; }
		}

		[Tooltip("If its unique container and can't be bothered to make SO")]
		public List<Reaction> AdditionalReactions = new List<Reaction>();

		private HashSet<Reaction> containedAdditionalReactions;


		//Includes everything on parents to, if needs to be dynamic can change
		public HashSet<Reaction> ContainedAdditionalReactions
		{
			get
			{
				lock (AdditionalReactions)
				{
					if (containedAdditionalReactions == null)
					{
						containedAdditionalReactions = new HashSet<Reaction>();
						foreach (var Reaction in AdditionalReactions)
						{
							containedAdditionalReactions.Add(Reaction);
						}
					}

					return containedAdditionalReactions;
				}
			}
		}

		[Tooltip("Initial mix of reagent inside container")]
		[FormerlySerializedAs("reagentMix")]
		[SerializeField]
		private ReagentMix initialReagentMix = new ReagentMix();

		[SerializeField] private bool destroyOnEmpty = default;

		private ItemAttributesV2 itemAttributes = default;
		private UniversalObjectPhysics ObjectPhysics;
		private Integrity integrity;


		public bool ContentsSet = false;

		[Tooltip("What options should appear on the right click menu.")] [SerializeField]
		private ShowMenuOptions menuOptions = ShowMenuOptions.All;

		/// <summary>
		/// Invoked server side when regent container spills all of its contents
		/// </summary>
		[NonSerialized] public UnityEvent OnSpillAllContents = new UnityEvent();

		/// <summary>
		/// Invoked server side when the mix of reagents inside container changes
		/// </summary>
		[NonSerialized] public UnityEvent OnReagentMixChanged = new UnityEvent();

		private IReagentMixProvider _customMixProviderProvider;


		private ReagentMix currentReagentMix;

		/// <summary>
		/// Server side only. Current reagent mix inside container.
		/// Invoke OnReagentMixChanged if you change anything in reagent mix
		/// </summary>
		public ReagentMix CurrentReagentMix
		{
			get
			{
				if (_customMixProviderProvider != null)
				{
					return _customMixProviderProvider.GetReagentMix();
				}

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

		private string FancyContainerName => itemAttributes ? itemAttributes.InitialName : gameObject.ExpensiveName();

		/// <summary>
		/// Server side only. Total reagent mix amount in units
		/// </summary>
		public float ReagentMixTotal => CurrentReagentMix.Total;

		[SerializeField] private SpriteHandler spriteHandler;

		private void Awake()
		{
			// register spill on throw
			ObjectPhysics = GetComponent<UniversalObjectPhysics>();
			if (ObjectPhysics)
			{
				ObjectPhysics.OnImpact.AddListener(OnImpact);
			}

			// spill all content on destroy
			integrity = GetComponent<Integrity>();
			if (integrity)
			{
				integrity.OnWillDestroyServer.AddListener(info => SpillAll());
			}
			//OnReagentMixChanged.AddListener(ReagentsChanged);

			if (spriteHandler == null) spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		public void SetSpriteColor(Color newColor)
		{
			spriteHandler.SetColor(newColor);
		}

		private void OnImpact(UniversalObjectPhysics UOP, Vector2 Momentum)
		{
			if (Momentum.magnitude > 1f)
			{
				//check spill on throw
				if (!Validations.HasItemTrait(this.gameObject, CommonTraits.Instance.SpillOnThrow) || IsEmpty)
				{
					return;
				}

				SpillAll(thrown: true);
			}
		}

		private HashSet<Reaction> possibleReactions = new HashSet<Reaction>();

		//Warning main thread only for now
		public void ReagentsChanged(bool applyChange = true)
		{
			possibleReactions.Clear();
			foreach (var reagents in CurrentReagentMix.reagents.m_dict)
			{
				var reactions = reagents.Key.RelatedReactions;
				int reactionsCount = reactions.Length;
				for (int i = 0; i < reactionsCount; i++)
				{
					var reaction = reactions[i];
					if (ReactionSet != null && ReactionSet.ContainedReactionss.Contains(reaction))
					{
						possibleReactions.Add(reaction);
					}
					else if (AdditionalReactions.Count > 0 && ContainedAdditionalReactions.Contains(reaction))
					{
						possibleReactions.Add(reaction);
					}
				}
			}

			if (applyChange == true) ReactionSet.Apply(this, CurrentReagentMix, possibleReactions);
			//ReactionSet.Apply(this, CurrentReagentMix,AdditionalReactions);
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

			OnReagentMixChanged?.Invoke();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			ContentsSet = false;
		}

		public void SetIProvideReagentMix(IReagentMixProvider inCustomMixProviderProvider)
		{
			_customMixProviderProvider = inCustomMixProviderProvider;
		}

		/// <summary>
		/// Server side only.
		/// Add reagent mix to container. May cause reaction inside container.
		/// Use MoveReagentsTo to transfer reagents from one container to another
		/// </summary>
		public TransferResult Add(ReagentMix addition, bool updateReactions = true)
		{
			// check whitelist reagents
			if (ReagentWhitelistOn)
			{
				lock (addition.reagents)
				{
					foreach (var reagent in addition.reagents.m_dict.Keys)
					{
						if (reagentWhitelist.Contains(reagent) == false)
						{
							return new TransferResult
							{
								Success = false,
								Message = "You can't transfer this into " + FancyContainerName
							};
						}
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

			// save total amount before mixing
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

			ReagentsChanged(updateReactions);

			// get mix total after all reactions,
			var afterReactionTotal = CurrentReagentMix.Total;

			var message = string.Empty;
			if (ReagentMixTotal > MaxCapacity)
			{
				//Reaction ends up in more reagents than container can hold
				CurrentReagentMix.Max(MaxCapacity, out _);
				message = $"Content starts overflowing out of {FancyContainerName}!";
			}

			ReagentsChanged(updateReactions);

			if (updateReactions == true) OnReagentMixChanged?.Invoke();

			return new TransferResult {Success = true, TransferAmount = transferAmount, Message = message};
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

		public void SetMaxCapacity(int Size)
		{
			MaxCapacity = Size;
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
					var worldPos = ObjectPhysics.transform.position.RoundToInt();
					worldPos.z = 0;
					SpillAll(worldPos, thrown);
				}
			}
			catch (NullReferenceException exception)
			{
				Loggy.LogError(
					$"Caught NRE in ReagentContainer SpillAll method: {exception.Message} \n {exception.StackTrace}",
					Category.Chemistry);
			}
		}

		/// <summary>
		/// Server side only.
		/// </summary>
		public void Spill(Vector3Int worldPos, float amount)
		{
			if (amount > ReagentMixTotal) SpillAll(worldPos);
			else
			{
				var spilledReagents = TakeReagents(amount);
				MatrixManager.ReagentReact(spilledReagents, worldPos);
			}
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
			if (mobs is List<LivingHealthMasterBase>)
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
				Chat.AddActionMsgToChat(gameObject,
					$"The {gameObject.ExpensiveName()}'s contents spill all over the floor!");
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
			if (menuOptions.HasFlag(ShowMenuOptions.ShowContents))
			{
				result.AddElement("Contents", OnExamineContentsClicked);
			}

			var pourOutContext = ContextMenuApply.ByLocalPlayer(gameObject, "PourOut");
			//Pour / add can only be done if in reach
			if (WillInteract(pourOutContext, NetworkSide.Client) && menuOptions.HasFlag(ShowMenuOptions.PourOut))
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
			var eyeItem = interaction.Performer.GetComponent<Equipment>().GetClothingItem(NamedSlot.eyes)
				.GameObjectReference;
			switch (interaction.RequestedOption)
			{
				case "Contents":
				{
					if (Validations.HasItemTrait(eyeItem, CommonTraits.Instance.ScienceScan))
					{
						eyeItem.GetComponent<ReagentScanner>()
							.DoScan(interaction.Performer.gameObject, this.gameObject);
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