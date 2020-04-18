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
public partial class ReagentContainer : MonoBehaviour, IRightClickable, IServerSpawn,
	IEnumerable<KeyValuePair<Chemistry.Reagent, float>>
{
	[Header("Container Parameters")]

	[Tooltip("Max container capacity in units")]
	[SerializeField] private int maxCapacity = 100;
	public int MaxCapacity => maxCapacity;

	[Tooltip("Reactions list which can happen inside container. Use Default for generic containers")]
	[SerializeField] private ReactionSet reactionSet;
	public ReactionSet ReactionSet => reactionSet;

	[FormerlySerializedAs("reagentMix")]
	[SerializeField] private ReagentMix initialReagentMix = new ReagentMix();

	private ItemAttributesV2 itemAttributes;
	private RegisterTile registerTile;
	private EmptyFullSync containerSprite;

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
	/// Server side only.
	/// Current reagent mix inside container.
	/// </summary>
	private ReagentMix CurrentReagentMix
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

	public float CurrentCapacity
	{
		get => currentCapacity;
		private set
		{
			currentCapacity = value;
			onCurrentCapacityChange.Invoke(value);
		}
	}

	public float? this[Chemistry.Reagent reagent] => CurrentReagentMix[reagent];

	private DictionaryReagentFloat Reagents => CurrentReagentMix.reagents;


	private FloatEvent onCurrentCapacityChange = new FloatEvent();

	public bool TraitWhitelistOn => traitWhitelist.Count > 0;



	public bool ReagentWhitelistOn => reagentWhitelist != null && reagentWhitelist.Count > 0;


	public bool IsFull => CurrentCapacity >= MaxCapacity;
	public bool IsEmpty => CurrentCapacity <= 0f;

	public float TransferAmount
	{
		get => transferAmount;
		set => transferAmount = value;
	}

	public float Temperature
	{
		get => CurrentReagentMix.Temperature;
		set
		{
			CurrentReagentMix.Temperature = value;
			OnReagentMixChanged?.Invoke();
		}
	}


	private float currentCapacity;

	private void Awake()
	{
		itemAttributes = GetComponent<ItemAttributesV2>();

		// add ReagentContainer trait on server and client
		if (itemAttributes != null)
		{
			var containerTrait = CommonTraits.Instance.ReagentContainer;
			if (!itemAttributes.HasTrait(containerTrait))
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

	protected void ResetContents()
	{
		currentReagentMix = initialReagentMix.Clone();
		CurrentCapacity = CurrentReagentMix.Total;

		OnReagentMixChanged?.Invoke();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
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

		CurrentCapacity = CurrentReagentMix.Total;
		var totalToAdd = CurrentReagentMix.Total;

		if (CurrentCapacity >= MaxCapacity)
		{
			return new TransferResult
			{
				Success = false,
				Message = $"The {gameObject.ExpensiveName()} is full."
			};
		}

		CurrentReagentMix.Add(addition);

		//Reactions happen here
		ReactionSet.Apply(this, CurrentReagentMix);
		CurrentCapacity = CurrentReagentMix.Total;
		Clean();

		var message = string.Empty;
		if (CurrentCapacity > MaxCapacity)
		{
			//Reaction ends up in more reagents than container can hold
			var excessCapacity = CurrentCapacity;
			CurrentReagentMix.Max(MaxCapacity, out _);
			CurrentCapacity = CurrentReagentMix.Total;
			message = $"Reaction caused excess ({excessCapacity}/{MaxCapacity}), current capacity is {CurrentCapacity}";
		}

		OnReagentMixChanged?.Invoke();
		return new TransferResult {Success = true, TransferAmount = totalToAdd, Message = message};
	}

	public bool Contains(Chemistry.Reagent reagent, float amount)
	{
		return CurrentReagentMix.Contains(reagent, amount);
	}

	public bool ContainsMoreThan(Chemistry.Reagent reagent, float amount)
	{
		return CurrentReagentMix.ContainsMoreThan(reagent, amount);
	}

	/// <summary>
	/// Extracts reagents to be used outside ReagentContainer
	/// </summary>
	public ReagentMix TakeReagents(float amount)
	{	
		var takeMix = CurrentReagentMix.Take(amount);
		OnReagentMixChanged?.Invoke();
		return takeMix;
	}

	public void Subtract(ReagentMix reagents)
	{
		CurrentReagentMix.Subtract(reagents);
		OnReagentMixChanged?.Invoke();
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

	private void SpillAll(bool thrown = false)
	{
		SpillAll(TransformState.HiddenPos, thrown);
	}

	public void Spill(Vector3Int worldPos, float amount)
	{
		var spilledReagents = TakeReagents(amount);
		CurrentCapacity = CurrentReagentMix.Total;
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
		var spilledReagents = TakeReagents(CurrentReagentMix.Total);
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

	private void ServerSpillInteraction(ReagentContainer reagentContainer, PlayerScript srcPlayer,
		PlayerScript dstPlayer)
	{
		if (reagentContainer.IsEmpty)
		{
			return;
		}

		SpillAll(dstPlayer.WorldPos);
	}

	public IEnumerator<KeyValuePair<Chemistry.Reagent, float>> GetEnumerator()
	{
		return CurrentReagentMix.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public override string ToString()
	{
		return $"[{gameObject.ExpensiveName()}" +
		       $" |{CurrentCapacity}/{MaxCapacity}|" +
		       $" ({string.Join(",", Reagents)})" +
		       $" Mode: {transferMode}," +
		       $" TransferAmount: {TransferAmount}," +
		       $" {nameof(IsEmpty)}: {IsEmpty}," +
		       $" {nameof(IsFull)}: {IsFull}" +
		       "]";
	}

	public void Multiply(float multiplier)
	{
		CurrentReagentMix.Multiply(multiplier);
		OnReagentMixChanged?.Invoke();
	}

	public void Clean()
	{
		CurrentReagentMix.Clean();
		OnReagentMixChanged?.Invoke();
	}


	public Color GetMixColor()
	{
		return CurrentReagentMix.MixColor;
	}

	/// <summary>
	/// Server only. Returns [0,1] fill percents
	/// </summary>
	/// <returns></returns>
	public float GetFillPercent()
	{
		if (IsEmpty)
			return 0f;

		return Mathf.Clamp01(CurrentReagentMix.Total / MaxCapacity);
	}

	/// <summary>
	/// Server only. Reagent mix ammount in units
	/// </summary>
	public float ReagentMixTotal
	{
		get
		{
			return CurrentReagentMix.Total;
		}
	}

	/// <summary>
	/// Server only. Reagent with biggest amount in mix
	/// </summary>
	public Chemistry.Reagent MajorMixReagent => CurrentReagentMix.MajorMixReagent;
}

