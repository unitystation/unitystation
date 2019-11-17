using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WebSocketSharp;

[RequireComponent(typeof(RightClickAppearance))]
public class ReagentContainer : Container, IRightClickable, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate> {
	public float CurrentCapacity
	{
		get => currentCapacity;
		private set
		{
			currentCapacity = value;
			OnCurrentCapacityChange.Invoke(value);
		}
	}
	private FloatEvent OnCurrentCapacityChange = new FloatEvent();
	public List<string> Reagents; //Specify reagent
	public List<float> Amounts;  //And how much
	public List<string> AcceptedReagents;
	public bool ReagentsFilterOn => AcceptedReagents.Count > 0;

	public TransferMode TransferMode = TransferMode.Normal;

	public bool IsFull => CurrentCapacity >= MaxCapacity;
	public bool IsEmpty => CurrentCapacity <= 0f;

	/// <summary>
	/// Server only
	/// </summary>
	[Range(1,100)]
	public float TransferAmount = 20;

	public List<float> PossibleTransferAmounts;

	private RegisterTile registerTile;
	private EmptyFullContainer containerSprite;
	private float currentCapacity;

	private void Awake()
	{
		var itemAttributes = GetComponent<ItemAttributes>();
		if ( itemAttributes )
		{
			itemAttributes.AddTrait(CommonTraits.Instance.ReagentContainer);
		}
		OnCurrentCapacityChange.AddListener(newCapacity =>
		{
			if (containerSprite == null)
			{
				return;
			}
			containerSprite.SyncSprite(IsEmpty ? EmptyFullStatus.Empty : EmptyFullStatus.Full);
		});
	}

	void Start() //Initialise the contents if there are any
	{
		if(Reagents == null)
		{
			return;
		}
		for (int i = 0; i < Reagents.Count; i++)
		{
			Contents[Reagents[i]] = Amounts[i];
		}
		CurrentCapacity = AmountOfReagents(Contents);

		registerTile = GetComponent<RegisterTile>();
		var customNetTransform = GetComponent<CustomNetTransform>();
		if (customNetTransform != null)
		{
			customNetTransform.OnThrowEnd.AddListener(CheckSpill);
		}

		if (containerSprite == null)
		{
			containerSprite = GetComponent<EmptyFullContainer>();
		}
	}
	
	private void CheckSpill(ThrowInfo throwInfo)
	{
		if (Validations.HasItemTrait(this.gameObject, CommonTraits.Instance.SpillOnThrow))
		{
			if (IsEmpty)
			{
				return;
			}
			SpillAll();
			Chat.AddLocalMsgToChat($"{gameObject.ExpensiveName()}'s contents spill all over the floor!", throwInfo.OriginPos);
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create();

		if ( CustomNetworkManager.Instance._isServer )
		{ //fixme: these only work on server
			//contents can always be viewed
			result.AddElement("Contents", LogReagents);

			//Pour / add can only be done if in reach
			if ( PlayerScript.IsInReach(registerTile, PlayerManager.LocalPlayerScript.registerTile, false))
			{
				result.AddElement( "PourOut", RemoveAll );
			}
		}

		return result;
	}

	public TransferResult AddReagents(Dictionary<string, float> reagents, float temperatureContainer)
	{

		if ( ReagentsFilterOn )
		{
			foreach ( var reagent in reagents.Keys )
			{
				if ( !AcceptedReagents.Contains( reagent ) )
				{
					return new TransferResult {Success = false, Message = "You can't transfer this into "+gameObject.ExpensiveName()};
				}
			}
		}

		CurrentCapacity = AmountOfReagents(Contents);
		float totalToAdd = AmountOfReagents(reagents);

		if ( CurrentCapacity >= MaxCapacity )
		{
			return new TransferResult {Success = false, Message = "The "+gameObject.ExpensiveName()+" is full."};
		}

		float divideAmount = Math.Min((MaxCapacity - CurrentCapacity), totalToAdd) / totalToAdd;
		foreach (KeyValuePair<string, float> reagent in reagents)
		{
			float amountToAdd = reagent.Value * divideAmount;
			Contents[reagent.Key] = (Contents.TryGetValue(reagent.Key, out float val) ? val : 0f) + amountToAdd;
		}

		float oldCapacity = CurrentCapacity;
		Contents = Calculations.Reactions(Contents, Temperature);
		CurrentCapacity = AmountOfReagents(Contents);
		Temperature = ((CurrentCapacity - oldCapacity) * temperatureContainer) + (oldCapacity * Temperature) / CurrentCapacity;

		return new TransferResult{ Success = true, TransferAmount = totalToAdd };
	}

	public TransferResult MoveReagentsTo(float amount, ReagentContainer target = null)
	{

		CurrentCapacity = AmountOfReagents(Contents);
		float toMove = target == null ? amount : Math.Min(target.MaxCapacity - target.CurrentCapacity, amount);
		float divideAmount = toMove / CurrentCapacity;

		var toTransfer = Contents.ToDictionary(
			reagent => reagent.Key,
			reagent => divideAmount > 1 ? reagent.Value : (reagent.Value * divideAmount)
		);

		TransferResult transferResult;

		if ( target != null )
		{
			transferResult = target.AddReagents(toTransfer, Temperature);
			if ( !transferResult.Success )
			{ //don't consume contents if transfer failed
				return transferResult;
			}
		}
		else
		{
			transferResult = new TransferResult {Success = true, TransferAmount = amount, Message = "Reagents were consumed"};
		}

		if ( transferResult.TransferAmount < toMove )
		{
			Logger.LogWarningFormat( "transfer amount {0} < toMove {1}, rebuilding divideAmount and toTransfer", 
				Category.Chemistry, transferResult.TransferAmount, toMove );
			divideAmount = transferResult.TransferAmount / CurrentCapacity;

			toTransfer = Contents.ToDictionary(
				reagent => reagent.Key,
				reagent => divideAmount > 1 ? reagent.Value : (reagent.Value * divideAmount)
			);
		}

		foreach(var reagent in toTransfer)
		{
			Contents[reagent.Key] -= reagent.Value;
		}
		Contents = Calculations.RemoveEmptyReagents(Contents);
		CurrentCapacity = AmountOfReagents(Contents);

		return transferResult;
	}

	public float AmountOfReagents(Dictionary<string, float> Reagents) => Reagents.Select(reagent => reagent.Value).Sum();

	private void LogReagents()
	{
		if (IsEmpty)
		{
			Logger.Log("It's empty", Category.Chemistry);
			return;
		}
		foreach (var reagent in Contents)
		{
			Logger.Log(reagent.Key + " at " + reagent.Value, Category.Chemistry);
		}
	}

	private void RemoveSome() => MoveReagentsTo(10);
	private void RemoveAll()
	{
		MoveReagentsTo(AmountOfReagents(Contents));
	}
	private void SpillAll()
	{
		MoveReagentsTo(AmountOfReagents(Contents));
	}

	public bool WillInteract( HandApply interaction, NetworkSide side )
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if ( interaction.HandObject == null || interaction.TargetObject == null ) return false;

		var srcContainer = interaction.HandObject.GetComponent<ReagentContainer>();
		var dstContainer = interaction.TargetObject.GetComponent<ReagentContainer>();

		if ( srcContainer == null || dstContainer == null ) return false;

		if ( srcContainer.TransferMode == TransferMode.NoTransfer
		     || dstContainer.TransferMode == TransferMode.NoTransfer )
		{
			return false;
		}

		if ( dstContainer.TransferMode == TransferMode.Syringe ) return false; //syringes can only be used from source

		return true;
	}

	/// <summary>
	/// Transfers Reagents between two containers
	/// </summary>
	/// <param name="interaction"></param>
	public void ServerPerformInteraction( HandApply interaction )
	{
		var one = interaction.HandObject.GetComponent<ReagentContainer>();
		var two = interaction.TargetObject.GetComponent<ReagentContainer>();

		ReagentContainer transferTo = null;
		switch ( one.TransferMode )
		{
			case TransferMode.Normal:
				switch ( two.TransferMode )
				{
					case TransferMode.Normal:
//						var sizeOne = one.registerTile.CustomTransform.Pushable.Size;
//						var sizeTwo = two.registerTile.CustomTransform.Pushable.Size;
						//experimental: one should output unless two is larger
						transferTo = two;//sizeOne >= sizeTwo ? two : one;
						break;
					case TransferMode.OutputOnly:
						transferTo = one;
						break;
					case TransferMode.InputOnly:
						transferTo = two;
						break;
					default:
						Logger.LogErrorFormat( "Invalid transfer mode when attempting transfer {0}<->{1}", Category.Chemistry, one, two );
						break;
				}
				break;
			case TransferMode.Syringe:
				switch ( two.TransferMode )
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
						Logger.LogErrorFormat( "Invalid transfer mode when attempting transfer {0}<->{1}", Category.Chemistry, one, two );
						break;
				}
				break;
			case TransferMode.OutputOnly:
				switch ( two.TransferMode )
				{
					case TransferMode.Normal:
						transferTo = two;
						break;
					case TransferMode.OutputOnly:
						Chat.AddExamineMsg( interaction.Performer, "Both containers are output-only." );
						break;
					case TransferMode.InputOnly:
						transferTo = two;
						break;
					default:
						Logger.LogErrorFormat( "Invalid transfer mode when attempting transfer {0}<->{1}", Category.Chemistry, one, two );
						break;
				}
				break;
			case TransferMode.InputOnly:
				switch ( two.TransferMode )
				{
					case TransferMode.Normal:
						transferTo = one;
						break;
					case TransferMode.OutputOnly:
						transferTo = one;
						break;
					case TransferMode.InputOnly:
						Chat.AddExamineMsg( interaction.Performer, "Both containers are input-only." );
						break;
					default:
						Logger.LogErrorFormat( "Invalid transfer mode when attempting transfer {0}<->{1}", Category.Chemistry, one, two );
						break;
				}
				break;
			default:
				Logger.LogErrorFormat( "Invalid transfer mode when attempting transfer {0}<->{1}", Category.Chemistry, one, two );
				break;
		}

		var transferFrom = two == transferTo ? one : two;

		Logger.LogTraceFormat( "Attempting transfer from {0} into {1}", Category.Chemistry, transferFrom, transferTo );

		if ( transferTo == null )
		{
			return;
		}

		if ( transferFrom.IsEmpty )
		{
			//red msg
			Chat.AddExamineMsg( interaction.Performer, "The "+transferFrom.gameObject.ExpensiveName()+" is empty!" );
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
		
		TransferResult result = transferFrom.MoveReagentsTo( transferAmount, transferTo );

		string resultMessage;
		if (string.IsNullOrEmpty(result.Message))
			resultMessage = useFillMessage ?
				$"You fill the {transferTo.gameObject.ExpensiveName()} with {result.TransferAmount} units of the contents of the {transferFrom.gameObject.ExpensiveName()}."
			  : $"You transfer {result.TransferAmount} units of the solution to the {transferTo.gameObject.ExpensiveName()}.";
		else
			resultMessage = result.Message;
		Chat.AddExamineMsg( interaction.Performer, resultMessage );
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
		Chat.AddExamineMsg( interaction.Performer,$"The {gameObject.ExpensiveName()}'s transfer amount is now {TransferAmount} units.");
	}

	public override string ToString()
	{
		return $"[ |{gameObject.ExpensiveName()}|" +
		       $" Mode: {TransferMode}," +
		       $" Amount: {TransferAmount}," +
		       $" Capacity: {CurrentCapacity}/{MaxCapacity}," +
		       $" {nameof( IsEmpty )}: {IsEmpty}," +
		       $" {nameof( IsFull )}: {IsFull}" +
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