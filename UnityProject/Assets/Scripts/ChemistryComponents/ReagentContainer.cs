using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Chemistry;

/// <summary>
/// Note that pretty much all the methods in this component work server-side only, but aren't
/// prefixed with "Server"
/// </summary>
[RequireComponent(typeof(RightClickAppearance))]
public class ReagentContainer : MonoBehaviour, IRightClickable, IServerSpawn,
	ICheckedInteractable<HandApply>, //Transfer: active hand <-> object in the world
	ICheckedInteractable<HandActivate>, //Activate to change transfer amount
	ICheckedInteractable<InventoryApply>, //Transfer: active hand <-> other hand
	IEnumerable<KeyValuePair<Chemistry.Reagent, float>>
{
	public int maxCapacity = 100;
	[SerializeField] private ReactionSet reactionSet;
	[SerializeField] private ReagentMix reagentMix = new ReagentMix();

	public float CurrentCapacity
	{
		get => currentCapacity;
		private set
		{
			currentCapacity = value;
			onCurrentCapacityChange.Invoke(value);
		}
	}

	public float? this[Chemistry.Reagent reagent] => reagentMix[reagent];

	private DictionaryReagentFloat Reagents => reagentMix.reagents;

	public ItemAttributesV2 itemAttributes;
	private FloatEvent onCurrentCapacityChange = new FloatEvent();

	[FormerlySerializedAs("TraitWhitelist")] [FormerlySerializedAs("AcceptedTraits")]
	public List<ItemTrait> traitWhitelist;

	public bool TraitWhitelistOn => traitWhitelist.Count > 0;

	[FormerlySerializedAs("ReagentWhitelist")] [FormerlySerializedAs("AcceptedReagents")]
	public List<Chemistry.Reagent> reagentWhitelist;

	public bool ReagentWhitelistOn => reagentWhitelist != null && reagentWhitelist.Count > 0;

	[FormerlySerializedAs("TransferMode")] public TransferMode transferMode = TransferMode.Normal;

	public bool IsFull => CurrentCapacity >= maxCapacity;
	public bool IsEmpty => CurrentCapacity <= 0f;

	[FormerlySerializedAs("PossibleTransferAmounts")]
	public List<float> possibleTransferAmounts;

	/// <summary>
	/// Invoked server side when regent container spills all of its contents
	/// </summary>
	[NonSerialized] public UnityEvent OnSpillAllContents = new UnityEvent();

	[field: Range(1, 100)]
	[field: SerializeField]
	[field: FormerlySerializedAs("TransferAmount")]
	[field: FormerlySerializedAs("InitialTransferAmount")]
	private float transferAmount = 20;

	public float TransferAmount
	{
		get => transferAmount;
		set => transferAmount = value;
	}

	public float Temperature
	{
		get => reagentMix.Temperature;
		set => reagentMix.Temperature = value;
	}

	public ReactionSet ReactionSet
	{
		get => reactionSet;
		set => reactionSet = value;
	}

	private RegisterTile registerTile;
	private EmptyFullSync containerSprite;
	private float currentCapacity;

	private void Awake()
	{
		itemAttributes = GetComponent<ItemAttributesV2>();
		if (itemAttributes != null)
		{
			itemAttributes.AddTrait(CommonTraits.Instance.ReagentContainer);
		}

		this.WaitForNetworkManager(() =>
		{
			if (!CustomNetworkManager.IsServer)
			{
				return;
			}

			onCurrentCapacityChange.AddListener(newCapacity =>
			{
				if (containerSprite == null)
				{
					return;
				}

				containerSprite.SetState(IsEmpty ? EmptyFullStatus.Empty : EmptyFullStatus.Full);
			});
		});
	}

	private void Start()
	{
		if (!CustomNetworkManager.IsServer)
		{
			return;
		}

		registerTile = GetComponent<RegisterTile>();
		var customNetTransform = GetComponent<CustomNetTransform>();
		if (customNetTransform != null)
		{
			customNetTransform.OnThrowEnd.AddListener(throwInfo =>
			{
				//check spill on throw
				if (!Validations.HasItemTrait(this.gameObject, CommonTraits.Instance.SpillOnThrow))
				{
					return;
				}

				if (IsEmpty)
				{
					return;
				}

				SpillAll(thrown: true);
			});
		}

		if (containerSprite == null)
		{
			containerSprite = GetComponent<EmptyFullSync>();
		}

		var integrity = GetComponent<Integrity>();
		if (integrity != null)
		{
			integrity.OnWillDestroyServer.AddListener(info => SpillAll());
		}
	}

	/// <summary>
	/// Reinitialise the contents if there are any with the passed in values
	/// </summary>
	/// <param name="reagents">List of reagent names</param>
	/// <param name="amounts">List of reagent amounts</param>
	public void ResetContents(List<string> reagents, List<float> amounts)
	{
		//Reagents = reagents;
		//Amounts = amounts;
		ResetContents();
	}

	protected void ResetContents()
	{
		CurrentCapacity = reagentMix.Total;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		TransferAmount = TransferAmount;
		ResetContents();
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (!CustomNetworkManager.Instance._isServer)
		{
			return result;
		}

		//fixme: these only work on server
		result.AddElement("Contents", ExamineContents);
		//Pour / add can only be done if in reach
		if (Validations.IsInReach(registerTile, PlayerManager.LocalPlayerScript.registerTile, false))
		{
			result.AddElement("PourOut", () => SpillAll());
		}

		return result;
	}

	private void ExamineContents()
	{
		if (IsEmpty)
		{
			Chat.AddExamineMsgToClient(gameObject.ExpensiveName() + " is empty.");
			return;
		}

		foreach (var reagent in Reagents)
		{
			Chat.AddExamineMsgToClient($"{gameObject.ExpensiveName()} contains {reagent.Value} {reagent.Key}.");
		}
	}

	/// <summary>
	/// Add reagents
	/// </summary>
	/// <param name="addition">Reagents to add</param>
	/// <param name="addTemperature">KELVIN temperature of added reagents, default equals to 20°C</param>
	/// <returns>Result of transfer. Can see how much was actually transferred, if any</returns>
	public TransferResult Add(ReagentMix addition)
	{
		if (ReagentWhitelistOn)
		{
			if (!addition.reagents.All(r => reagentWhitelist.Contains(r.Key)))
			{
				return new TransferResult
				{
					Success = false,
					Message = "You can't transfer this into " + gameObject.ExpensiveName()
				};
			}
		}

		CurrentCapacity = reagentMix.Total;
		var totalToAdd = reagentMix.Total;

		if (CurrentCapacity >= maxCapacity)
		{
			return new TransferResult
			{
				Success = false,
				Message = $"The {gameObject.ExpensiveName()} is full."
			};
		}

		reagentMix.Add(addition);

		//Reactions happen here
		reactionSet.Apply(this, reagentMix);
		CurrentCapacity = reagentMix.Total;
		reagentMix.Clean();

		var message = string.Empty;
		if (CurrentCapacity > maxCapacity)
		{
			//Reaction ends up in more reagents than container can hold
			var excessCapacity = CurrentCapacity;
			reagentMix.Max(maxCapacity, out _);
			CurrentCapacity = reagentMix.Total;
			message = $"Reaction caused excess ({excessCapacity}/{maxCapacity}), current capacity is {CurrentCapacity}";
		}

		return new TransferResult {Success = true, TransferAmount = totalToAdd, Message = message};
	}

	public bool Contains(Chemistry.Reagent reagent, float amount)
	{
		return reagentMix.Contains(reagent, amount);
	}

	public bool ContainsMoreThan(Chemistry.Reagent reagent, float amount)
	{
		return reagentMix.ContainsMoreThan(reagent, amount);
	}

	/// <summary>
	/// Extracts reagents to be used outside ReagentContainer
	/// </summary>
	public ReagentMix TakeReagents(float amount)
	{
		return reagentMix.Take(amount);
	}

	public void Subtract(ReagentMix reagents)
	{
		reagentMix.Subtract(reagents);
	}

	/// <summary>
	/// Moves reagents to another container
	/// </summary>
	public TransferResult MoveReagentsTo(float amount, ReagentContainer target)
	{
		return TransferTo(amount, target);
	}

	/// <summary>
	/// Moves reagents to another container
	/// </summary>
	public TransferResult TransferTo(
		float amount,
		ReagentContainer target
	)
	{
		TransferResult transferResult;

		if (target != null)
		{
			var transffered = reagentMix.Take(amount);
			transferResult = target.Add(transffered);
			if (!transferResult.Success)
			{
				//don't consume contents if transfer failed
				return transferResult;
			}
		}
		else
		{
			transferResult = new TransferResult
			{
				Success = true,
				TransferAmount = amount,
				Message = "Reagents were consumed"
			};
		}

		reagentMix.Clean();
		CurrentCapacity = reagentMix.Total;

		return transferResult;
	}

	/// <summary>
	/// Gets the amount of a particular reagent. 0 if it doesn't have this reagent.
	/// </summary>
	/// <param name="reagentName"></param>
	/// <returns></returns>
	public float AmountOfReagent(Chemistry.Reagent reagent)
	{
		Reagents.TryGetValue(reagent, out var amount);
		return amount;
	}

	private void RemoveAll()
	{
		reagentMix.Clear();
	}

	private void SpillAll(bool thrown = false)
	{
		SpillAll(TransformState.HiddenPos, thrown);
	}

	public void Spill(Vector3Int worldPos, float amount)
	{
		var spilledReagents = TakeReagents(amount);
		CurrentCapacity = reagentMix.Total;
		MatrixManager.ReagentReact(spilledReagents, worldPos);
	}

	private void SpillAll(Vector3Int worldPos, bool thrown = false)
	{
		if (IsEmpty)
		{
			return;
		}

		//It could of been destroyed in an explosion:
		if (registerTile.CustomTransform == null)
		{
			return;
		}

		if (worldPos == TransformState.HiddenPos)
		{
			worldPos = registerTile.CustomTransform.AssumedWorldPositionServer().CutToInt();
		}

		NotifyPlayersOfSpill(worldPos);

		//todo: half decent regs spread
		var spilledReagents = TakeReagents(reagentMix.Total);
		MatrixManager.ReagentReact(spilledReagents, worldPos);

		OnSpillAllContents.Invoke();
	}

	private void NotifyPlayersOfSpill(Vector3Int worldPos)
	{
		var mobs = MatrixManager.GetAt<LivingHealthBehaviour>(worldPos, true);
		if (mobs.Count > 0)
		{
			foreach (var mob in mobs)
			{
				var mobGameObject = mob.gameObject;
				Chat.AddCombatMsgToChat(mobGameObject, mobGameObject.name + " has been splashed with something!",
					mobGameObject.name + " has been splashed with something!");
			}
		}
		else
		{
			Chat.AddLocalMsgToChat($"{gameObject.ExpensiveName()}'s contents spill all over the floor!",
				(Vector3) worldPos, gameObject);
		}
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		return WillInteractHelp(interaction.UsedObject, interaction.TargetObject, side);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		var playerScript = interaction.Performer.GetComponent<PlayerScript>();
		if (!playerScript) return false;

		if (interaction.Intent == Intent.Help)
		{
			//checks if it's possible to transfer from container to container
			if (!WillInteractHelp(interaction.HandObject, interaction.TargetObject, side)) return false;
		}
		else
		{
			//checks if it's possible to spill contents on player
			if (!WillInteractHarm(interaction.HandObject, interaction.TargetObject, side)) return false;
		}

		return true;
	}

	private bool WillInteractHarm(GameObject srcObject, GameObject dstObject, NetworkSide side)
	{
		if (srcObject == null || dstObject == null) return false;

		var srcContainer = srcObject.GetComponent<ReagentContainer>();

		if (srcContainer == null) return false;

		if (srcContainer.transferMode == TransferMode.NoTransfer) return false;

		if (dstObject.GetComponent<PlayerScript>() == null) return false;

		return true;
	}

	private bool WillInteractHelp(GameObject srcObject, GameObject dstObject, NetworkSide side)
	{
		if (srcObject == null || dstObject == null) return false;

		var srcContainer = srcObject.GetComponent<ReagentContainer>();
		var dstContainer = dstObject.GetComponent<ReagentContainer>();

		if (srcContainer == null || dstContainer == null) return false;

		if (srcContainer.transferMode == TransferMode.NoTransfer
		    || dstContainer.transferMode == TransferMode.NoTransfer)
		{
			return false;
		}

		if (side == NetworkSide.Server)
		{
			if (srcContainer.TraitWhitelistOn && !Validations.HasAnyTrait(dstObject, srcContainer.traitWhitelist))
			{
				return false;
			}

			if (dstContainer.TraitWhitelistOn && !Validations.HasAnyTrait(dstObject, srcContainer.traitWhitelist))
			{
				return false;
			}
		}

		return dstContainer.transferMode != TransferMode.Syringe;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		var one = interaction.UsedObject.GetComponent<ReagentContainer>();
		var two = interaction.TargetObject.GetComponent<ReagentContainer>();

		ServerTransferInteraction(one, two, interaction.Performer);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var srcPlayer = interaction.Performer.GetComponent<PlayerScript>();

		if (interaction.Intent == Intent.Help)
		{
			var one = interaction.HandObject.GetComponent<ReagentContainer>();
			var two = interaction.TargetObject.GetComponent<ReagentContainer>();

			ServerTransferInteraction(one, two, interaction.Performer);
		}
		else
		{
			//TODO: Move this to Spill right click interaction? Need to make 'RequestSpill'
			var dstPlayer = interaction.TargetObject.GetComponent<PlayerScript>();
			ServerSpillInteraction(this, srcPlayer, dstPlayer);
		}
	}

	private void ServerSpillInteraction(ReagentContainer reagentContainer, PlayerScript srcPlayer,
		PlayerScript dstPlayer)
	{
		if (reagentContainer.IsEmpty)
		{
			return;
		}

		SpillAll(dstPlayer.WorldPos);
	}

	/// <summary>
	/// Transfers Reagents between two containers
	/// </summary>
	private void ServerTransferInteraction(ReagentContainer one, ReagentContainer two, GameObject performer)
	{
		ReagentContainer transferTo = null;
		switch (one.transferMode)
		{
			case TransferMode.Normal:
				switch (two.transferMode)
				{
					case TransferMode.Normal:
						transferTo = two;
						break;
					case TransferMode.OutputOnly:
						transferTo = one;
						break;
					case TransferMode.InputOnly:
						transferTo = two;
						break;
					default:
						Logger.LogErrorFormat("Invalid transfer mode when attempting transfer {0}<->{1}",
							Category.Chemistry, one, two);
						break;
				}

				break;
			case TransferMode.Syringe:
				switch (two.transferMode)
				{
					case TransferMode.Normal:
						transferTo = one.IsFull ? two : one;
						break;
					case TransferMode.OutputOnly:
						transferTo = one;
						break;
					case TransferMode.InputOnly:
						transferTo = two;
						break;
					default:
						Logger.LogErrorFormat("Invalid transfer mode when attempting transfer {0}<->{1}",
							Category.Chemistry, one, two);
						break;
				}

				break;
			case TransferMode.OutputOnly:
				switch (two.transferMode)
				{
					case TransferMode.Normal:
						transferTo = two;
						break;
					case TransferMode.OutputOnly:
						Chat.AddExamineMsg(performer, "Both containers are output-only.");
						break;
					case TransferMode.InputOnly:
						transferTo = two;
						break;
					default:
						Logger.LogErrorFormat("Invalid transfer mode when attempting transfer {0}<->{1}",
							Category.Chemistry, one, two);
						break;
				}

				break;
			case TransferMode.InputOnly:
				switch (two.transferMode)
				{
					case TransferMode.Normal:
						transferTo = one;
						break;
					case TransferMode.OutputOnly:
						transferTo = one;
						break;
					case TransferMode.InputOnly:
						Chat.AddExamineMsg(performer, "Both containers are input-only.");
						break;
					default:
						Logger.LogErrorFormat("Invalid transfer mode when attempting transfer {0}<->{1}",
							Category.Chemistry, one, two);
						break;
				}

				break;
			default:
				Logger.LogErrorFormat("Invalid transfer mode when attempting transfer {0}<->{1}", Category.Chemistry,
					one,
					two);
				break;
		}

		if (transferTo == null)
		{
			return;
		}

		var transferFrom = two == transferTo ? one : two;

		Logger.LogTraceFormat("Attempting transfer from {0} into {1}", Category.Chemistry, transferFrom, transferTo);


		if (transferFrom.IsEmpty)
		{
			//red msg
			Chat.AddExamineMsg(performer, "The " + transferFrom.gameObject.ExpensiveName() + " is empty!");
			return;
		}

		var transferAmount = transferFrom.TransferAmount;

		var useFillMessage = false;
		//always taking max capacity from output-only things like tanks
		if (transferFrom.transferMode == TransferMode.OutputOnly)
		{
			transferAmount = transferFrom.CurrentCapacity;
			useFillMessage = true;
		}

		var result = transferFrom.MoveReagentsTo(transferAmount, transferTo);

		string resultMessage;
		if (string.IsNullOrEmpty(result.Message))
			resultMessage = useFillMessage
				? $"You fill the {transferTo.gameObject.ExpensiveName()} with {result.TransferAmount} units of the contents of the {transferFrom.gameObject.ExpensiveName()}."
				: $"You transfer {result.TransferAmount} units of the solution to the {transferTo.gameObject.ExpensiveName()}.";
		else
			resultMessage = result.Message;
		Chat.AddExamineMsg(performer, resultMessage);
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return possibleTransferAmounts.Count != 0;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		var currentIndex = possibleTransferAmounts.IndexOf(TransferAmount);
		if (currentIndex != -1)
		{
			TransferAmount = possibleTransferAmounts.Wrap(currentIndex + 1);
		}
		else
		{
			TransferAmount = possibleTransferAmounts[0];
		}

		Chat.AddExamineMsg(interaction.Performer,
			$"The {gameObject.ExpensiveName()}'s transfer amount is now {TransferAmount} units.");
	}

	public IEnumerator<KeyValuePair<Chemistry.Reagent, float>> GetEnumerator()
	{
		return reagentMix.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public override string ToString()
	{
		return $"[{gameObject.ExpensiveName()}" +
		       $" |{CurrentCapacity}/{maxCapacity}|" +
		       $" ({string.Join(",", Reagents)})" +
		       $" Mode: {transferMode}," +
		       $" TransferAmount: {TransferAmount}," +
		       $" {nameof(IsEmpty)}: {IsEmpty}," +
		       $" {nameof(IsFull)}: {IsFull}" +
		       "]";
	}

	public void Multiply(float multiplier)
	{
		reagentMix.Multiply(multiplier);
	}

	public void Clean()
	{
		reagentMix.Clean();
	}
}

public struct TransferResult
{
	public bool Success;
	public string Message;
	public float TransferAmount;
}

public enum TransferMode
{
	Normal = 0, //Output from your hand, unless other thing is physically larger
	Syringe = 1, //Outputs if it's full, Inputs if it's empty
	OutputOnly = 2,
	InputOnly = 3,
	NoTransfer = 4
}