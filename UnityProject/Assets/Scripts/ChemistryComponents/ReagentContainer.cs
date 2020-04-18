using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Chemistry;

/// <summary>
/// Defines reagent container that can store reagent mix inside
/// All reagent mix logic done server-side
/// </summary>
[RequireComponent(typeof(RightClickAppearance))]
public partial class ReagentContainer : MonoBehaviour, IServerSpawn,
	IEnumerable<KeyValuePair<Chemistry.Reagent, float>>
{
	[Header("Container Parameters")]

	[Tooltip("Max container capacity in units")]
	[SerializeField] private int maxCapacity = 100;
	public int MaxCapacity => maxCapacity;

	[Tooltip("Reactions list which can happen inside container. Use Default for generic containers")]
	[SerializeField] private ReactionSet reactionSet;
	public ReactionSet ReactionSet => reactionSet;

	[Tooltip("Initial mix of reagent inside container")]
	[FormerlySerializedAs("reagentMix")]
	[SerializeField] private ReagentMix initialReagentMix = new ReagentMix();

	private ItemAttributesV2 itemAttributes;
	private RegisterTile registerTile;
	private CustomNetTransform customNetTransform;
	private Integrity integrity;

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

	/// <summary>
	/// Returns reagent amount in container. Null if reagent amount is 0
	/// </summary>
	public float? this[Chemistry.Reagent reagent] => CurrentReagentMix[reagent];

	/// <summary>
	/// Shortcut to reagents dictionary in reagent mix
	/// Invoke OnReagentMixChanged if you change anything in reagent mix
	/// </summary>
	private DictionaryReagentFloat Reagents => CurrentReagentMix.reagents;

	public bool IsFull => ReagentMixTotal >= MaxCapacity;

	public bool IsEmpty => ReagentMixTotal <= 0f;

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

	private string FancyContainerName
	{
		get
		{
			return itemAttributes ? itemAttributes.InitialName : gameObject.ExpensiveName();
		}
	}
		
	/// <summary>
	/// Server only. Total reagent mix ammount in units
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
		registerTile = GetComponent<RegisterTile>();

		// add ReagentContainer trait on server and client
		itemAttributes = GetComponent<ItemAttributesV2>();
		if (itemAttributes)
		{
			var containerTrait = CommonTraits.Instance.ReagentContainer;
			if (!itemAttributes.HasTrait(containerTrait))
				itemAttributes.AddTrait(CommonTraits.Instance.ReagentContainer);
		}

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
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		// reset all content on server respawn
		ResetContents();
	}

	protected void ResetContents()
	{
		currentReagentMix = initialReagentMix.Clone();
		OnReagentMixChanged?.Invoke();
	}

	/// <summary>
	/// Add reagent mix to container. May cause reaction.
	/// </summary>
	public TransferResult Add(ReagentMix addition)
	{
		// check whitelist
		if (ReagentWhitelistOn)
		{
			if (!addition.reagents.All(r => reagentWhitelist.Contains(r.Key)))
			{
				return new TransferResult
				{
					Success = false,
					Message = "You can't transfer this into " + FancyContainerName
				};
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

		// save total ammount before reaction
		var beforeMixTotal = CurrentReagentMix.Total;
		// add addition to reagent mix
		CurrentReagentMix.Add(addition);
		//Reactions happen here
		ReactionSet.Apply(this, CurrentReagentMix);

		var message = string.Empty;
		if (ReagentMixTotal > MaxCapacity)
		{
			//Reaction ends up in more reagents than container can hold
			var excessCapacity = ReagentMixTotal;
			CurrentReagentMix.Max(MaxCapacity, out _);
			ReagentMixTotal = CurrentReagentMix.Total;
			message = $"Reaction caused excess ({excessCapacity}/{MaxCapacity}), current capacity is {ReagentMixTotal}";
		}

		OnReagentMixChanged?.Invoke();

		var totalToAdd = CurrentReagentMix.Total;
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
		ReagentMixTotal = CurrentReagentMix.Total;
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
		       $" |{ReagentMixTotal}/{MaxCapacity}|" +
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
	/// Server only. Reagent with biggest amount in mix
	/// </summary>
	public Chemistry.Reagent MajorMixReagent => CurrentReagentMix.MajorMixReagent;
}

