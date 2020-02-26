using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using WebSocketSharp;

/// <summary>
/// Note that pretty much all the methods in this component work server-side only, but aren't
/// prefixed with "Server"
/// </summary>
[RequireComponent(typeof(RightClickAppearance))]
public class ReagentContainer : Container, IRightClickable, IServerSpawn,
	ICheckedInteractable<HandApply>, //Transfer: active hand <-> object in the world
	ICheckedInteractable<HandActivate>, //Activate to change transfer amount
	ICheckedInteractable<InventoryApply> //Transfer: active hand <-> other hand
{
	private const float REGS_PER_TILE = 10f;

	public float CurrentCapacity
	{
		get => currentCapacity;
		private set
		{
			currentCapacity = value;
			OnCurrentCapacityChange.Invoke(value);
		}
	}

	public ItemAttributesV2 itemAttributes;
	private FloatEvent OnCurrentCapacityChange = new FloatEvent();
	public bool InSolidForm;

	//Initial values for reagents:
	public List<string> Reagents; //Specify reagent
	public List<float> Amounts;  //And how much

	[FormerlySerializedAs("AcceptedTraits")]
	public List<ItemTrait> TraitWhitelist;
	public bool TraitWhitelistOn => TraitWhitelist.Count > 0;
	[FormerlySerializedAs("AcceptedReagents")]
	public List<string> ReagentWhitelist;
	public bool ReagentWhitelistOn => ReagentWhitelist != null && ReagentWhitelist.Count > 0;

	public TransferMode TransferMode = TransferMode.Normal;

	public bool IsFull => CurrentCapacity >= MaxCapacity;
	public bool IsEmpty => CurrentCapacity <= 0f;

	public float TransferAmount { get; private set; } = 20;

	public List<float> PossibleTransferAmounts;

	/// <summary>
	/// Invoked server side when regent container spills all of its contents
	/// </summary>
	[NonSerialized]
	public UnityEvent OnSpillAllContents = new UnityEvent();

	[Range(1, 100)]
	[SerializeField]
	[FormerlySerializedAs(nameof(TransferAmount))]
	private float InitialTransferAmount = 20;

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

			OnCurrentCapacityChange.AddListener(newCapacity =>
			{
				if (containerSprite == null)
				{
					return;
				}
				containerSprite.SetState(IsEmpty ? EmptyFullStatus.Empty : EmptyFullStatus.Full);
			});
		});
	}

	void Start()
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
			{ //check spill on throw
				if (Validations.HasItemTrait(this.gameObject, CommonTraits.Instance.SpillOnThrow))
				{
					if (IsEmpty)
					{
						return;
					}
					SpillAll(thrown: true);
				}
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
	/// (Re)initialise the contents if there are any
	/// </summary>
	protected override void ResetContents()
	{
		base.ResetContents();
		for (int i = 0; i < Reagents.Count; i++)
		{
			Contents[Reagents[i]] = Amounts[i];
		}
		CurrentCapacity = AmountOfReagents(Contents);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		TransferAmount = InitialTransferAmount;
		ResetContents();
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if (CustomNetworkManager.Instance._isServer)
		{
			//fixme: these only work on server
			result.AddElement("Contents", ExamineContents);
			//Pour / add can only be done if in reach
			if (Validations.IsInReach(registerTile, PlayerManager.LocalPlayerScript.registerTile, false))
			{
				result.AddElement("PourOut", () => SpillAll());
			}
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

		foreach (var reagent in Contents)
		{
			Chat.AddExamineMsgToClient(gameObject.ExpensiveName() + " contains " + reagent.Value + " " + reagent.Key + ".");
		}
	}

	public bool Contains(string Chemical, float Amount)
	{
		if (Contents.ContainsKey(Chemical))
		{
			if (Contents[Chemical] >= Amount)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Add reagents
	/// </summary>
	/// <param name="reagents">Reagents to add</param>
	/// <returns>Result of transfer. Can see how much was actually transferred, if any</returns>
	public TransferResult AddReagents(Dictionary<string, float> reagents)
	{
		return AddReagentsKelvin(reagents);
	}

	/// <summary>
	/// Add reagents
	/// </summary>
	/// <param name="reagents">Reagents to add</param>
	/// <param name="addTemperature">CELSIUS temperature of added reagents, default equals to 20°C</param>
	/// <returns>Result of transfer. Can see how much was actually transferred, if any</returns>
	public TransferResult AddReagentsCelsius(Dictionary<string, float> reagents, float addTemperature = 20f)
	{
		return AddReagentsKelvin(reagents, addTemperature + ZERO_CELSIUS_IN_KELVIN);
	}

	/// <summary>
	/// Add reagents
	/// </summary>
	/// <param name="reagents">Reagents to add</param>
	/// <param name="addTemperature">KELVIN temperature of added reagents, default equals to 20°C</param>
	/// <returns>Result of transfer. Can see how much was actually transferred, if any</returns>
	public TransferResult AddReagentsKelvin(Dictionary<string, float> reagents, float addTemperature = 293.15f)
	{

		if (ReagentWhitelistOn)
		{
			foreach (var reagent in reagents.Keys)
			{
				if (!ReagentWhitelist.Contains(reagent))
				{
					return new TransferResult { Success = false, Message = "You can't transfer this into " + gameObject.ExpensiveName() };
				}
			}
		}

		CurrentCapacity = AmountOfReagents(Contents);
		float totalToAdd = AmountOfReagents(reagents);

		if (CurrentCapacity >= MaxCapacity)
		{
			return new TransferResult { Success = false, Message = "The " + gameObject.ExpensiveName() + " is full." };
		}

		float divideAmount = Math.Min((MaxCapacity - CurrentCapacity), totalToAdd) / totalToAdd;
		foreach (KeyValuePair<string, float> reagent in reagents)
		{
			float amountToAdd = reagent.Value * divideAmount;
			Contents[reagent.Key] = (Contents.TryGetValue(reagent.Key, out float val) ? val : 0f) + amountToAdd;
		}

		float oldCapacity = CurrentCapacity;

		//Reactions happen here
		Contents = Calculations.Reactions(Contents, TemperatureKelvin);
		CurrentCapacity = AmountOfReagents(Contents);

		string message = string.Empty;
		if (CurrentCapacity > MaxCapacity)
		{ //Reaction ends up in more reagents than container can hold
			var excessCapacity = CurrentCapacity;
			Contents = Calculations.RemoveExcess(Contents, MaxCapacity);
			CurrentCapacity = AmountOfReagents(Contents);
			message = $"Reaction caused excess ({excessCapacity}/{MaxCapacity}), current capacity is {CurrentCapacity}";
		}

		TemperatureKelvin = (((CurrentCapacity - oldCapacity) * addTemperature) + (oldCapacity * TemperatureKelvin)) / CurrentCapacity;

		return new TransferResult { Success = true, TransferAmount = totalToAdd, Message = message };
	}

	/// <summary>
	/// Extracts reagents to be used outside ReagentContainer
	/// </summary>
	public Dictionary<string, float> TakeReagents(float amount)
	{
		MoveReagentsTo(amount, null, out var transferredReagents);
		return transferredReagents;
	}

	/// <summary>
	/// Moves reagents to another container
	/// </summary>
	public TransferResult MoveReagentsTo(float amount, ReagentContainer target)
	{
		return MoveReagentsTo(amount, target, out _);
	}

	/// <summary>
	/// Moves reagents to another container
	/// </summary>
	public TransferResult MoveReagentsTo(float amount, ReagentContainer target, out Dictionary<string, float> transferredReagents)
	{

		CurrentCapacity = AmountOfReagents(Contents);
		float toMove = target == null ? amount : Math.Min(target.MaxCapacity - target.CurrentCapacity, amount);
		float divideAmount = toMove / CurrentCapacity;

		transferredReagents = Contents.ToDictionary(
			reagent => reagent.Key,
			reagent => divideAmount >= 1 ? reagent.Value : (reagent.Value * divideAmount)
		);

		TransferResult transferResult;

		if (target != null)
		{
			transferResult = target.AddReagentsKelvin(transferredReagents, TemperatureKelvin);
			if (!transferResult.Success)
			{ //don't consume contents if transfer failed
				return transferResult;
			}
		}
		else
		{
			transferResult = new TransferResult { Success = true, TransferAmount = amount, Message = "Reagents were consumed" };
		}

		if (transferResult.TransferAmount < toMove)
		{
			Logger.LogWarningFormat("transfer amount {0} < toMove {1}, rebuilding divideAmount and toTransfer",
				Category.Chemistry, transferResult.TransferAmount, toMove);
			divideAmount = transferResult.TransferAmount / CurrentCapacity;

			transferredReagents = Contents.ToDictionary(
				reagent => reagent.Key,
				reagent => divideAmount > 1 ? reagent.Value : (reagent.Value * divideAmount)
			);
		}

		foreach (var reagent in transferredReagents)
		{
			Contents[reagent.Key] -= reagent.Value;
		}
		Contents = Calculations.RemoveEmptyReagents(Contents);
		CurrentCapacity = AmountOfReagents(Contents);

		return transferResult;
	}

	public float AmountOfReagents(Dictionary<string, float> Reagents) => Reagents.Select(reagent => reagent.Value).Sum();

	/// <summary>
	/// Gets the amount of a particular reagent. 0 if it doesn't have this reagent.
	/// </summary>
	/// <param name="reagentName"></param>
	/// <returns></returns>
	public float AmountOfReagent(string reagentName)
	{
		Contents.TryGetValue(reagentName, out var amount);
		return amount;
	}

	private void RemoveAll()
	{
		TakeReagents(AmountOfReagents(Contents));
	}

	private void SpillAll(bool thrown = false)
	{
		SpillAll(TransformState.HiddenPos, thrown);
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
		var spilledReagents = TakeReagents(AmountOfReagents(Contents));
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
			Chat.AddLocalMsgToChat($"{gameObject.ExpensiveName()}'s contents spill all over the floor!", (Vector3)worldPos, gameObject);
		}
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!WillInteractHelp(interaction.UsedObject, interaction.TargetObject, side)) return false;

		return true;
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		var playerScript = interaction.Performer.GetComponent<PlayerScript>();
		if (!playerScript) return false;

		if (interaction.Intent == Intent.Help)
		{ //checks if it's possible to transfer from container to container
			if (!WillInteractHelp(interaction.HandObject, interaction.TargetObject, side)) return false;
		}
		else
		{ //checks if it's possible to spill contents on player
			if (!WillInteractHarm(interaction.HandObject, interaction.TargetObject, side)) return false;
		}

		return true;
	}

	private bool WillInteractHarm(GameObject srcObject, GameObject dstObject, NetworkSide side)
	{
		if (srcObject == null || dstObject == null) return false;

		var srcContainer = srcObject.GetComponent<ReagentContainer>();

		if (srcContainer == null) return false;

		if (srcContainer.TransferMode == TransferMode.NoTransfer) return false;

		if (dstObject.GetComponent<PlayerScript>() == null) return false;

		return true;
	}

	private bool WillInteractHelp(GameObject srcObject, GameObject dstObject, NetworkSide side)
	{
		if (srcObject == null || dstObject == null) return false;

		var srcContainer = srcObject.GetComponent<ReagentContainer>();
		var dstContainer = dstObject.GetComponent<ReagentContainer>();

		if (srcContainer == null || dstContainer == null) return false;

		if (srcContainer.TransferMode == TransferMode.NoTransfer
			|| dstContainer.TransferMode == TransferMode.NoTransfer)
		{
			return false;
		}

		if (side == NetworkSide.Server)
		{
			if (srcContainer.TraitWhitelistOn && !Validations.HasAnyTrait(dstObject, srcContainer.TraitWhitelist))
			{
				return false;
			}
			if (dstContainer.TraitWhitelistOn && !Validations.HasAnyTrait(dstObject, srcContainer.TraitWhitelist))
			{
				return false;
			}
		}

		if (dstContainer.TransferMode == TransferMode.Syringe) return false;
		return true;
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

	private void ServerSpillInteraction(ReagentContainer reagentContainer, PlayerScript srcPlayer, PlayerScript dstPlayer)
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
		switch (one.TransferMode)
		{
			case TransferMode.Normal:
				switch (two.TransferMode)
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
				switch (two.TransferMode)
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
				switch (two.TransferMode)
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
				switch (two.TransferMode)
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
				Logger.LogErrorFormat("Invalid transfer mode when attempting transfer {0}<->{1}", Category.Chemistry, one,
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

		bool useFillMessage = false;
		//always taking max capacity from output-only things like tanks
		if (transferFrom.TransferMode == TransferMode.OutputOnly)
		{
			transferAmount = transferFrom.CurrentCapacity;
			useFillMessage = true;
		}

		TransferResult result = transferFrom.MoveReagentsTo(transferAmount, transferTo);

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

		if (PossibleTransferAmounts.Count == 0) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		int currentIndex = PossibleTransferAmounts.IndexOf(TransferAmount);
		if (currentIndex != -1)
		{
			TransferAmount = PossibleTransferAmounts.Wrap(currentIndex + 1);
		}
		else
		{
			TransferAmount = PossibleTransferAmounts[0];
		}
		Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()}'s transfer amount is now {TransferAmount} units.");
	}

	public override string ToString()
	{
		return $"[{gameObject.ExpensiveName()}" +
			   $" |{CurrentCapacity}/{MaxCapacity}|" +
			   $" ({string.Join(",", Contents)})" +
			   $" Mode: {TransferMode}," +
			   $" TransferAmount: {TransferAmount}," +
			   $" {nameof(IsEmpty)}: {IsEmpty}," +
			   $" {nameof(IsFull)}: {IsFull}" +
			   "]";
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