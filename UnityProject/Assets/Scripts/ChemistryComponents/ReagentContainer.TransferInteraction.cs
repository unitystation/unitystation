using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using UnityEngine.Serialization;

public struct TransferResult
{
	public bool Success;
	public string Message;
	public float TransferAmount;
	public ReagentMix Excess;
}

public enum TransferMode
{
	Normal = 0, //Output from your hand, unless other thing is physically larger
	Syringe = 1, //Outputs if it's full, Inputs if it's empty
	OutputOnly = 2,
	InputOnly = 3,
	NoTransfer = 4
}

public partial class ReagentContainer
{
	[Header("Transfer settings")]

	[Tooltip("If not empty, another container should have one of this traits to interact")]
	[FormerlySerializedAs("TraitWhitelist")]
	[FormerlySerializedAs("AcceptedTraits")]
	[SerializeField] private List<ItemTrait> traitWhitelist;

	[Tooltip("If not empty, only listed reagents can be inside container")]
	[FormerlySerializedAs("ReagentWhitelist")]
	[FormerlySerializedAs("AcceptedReagents")]
	[SerializeField] private List<Chemistry.Reagent> reagentWhitelist;

	[FormerlySerializedAs("TransferMode")]
	[SerializeField] private TransferMode transferMode = TransferMode.Normal;

	[FormerlySerializedAs("PossibleTransferAmounts")]
	[SerializeField] private List<float> possibleTransferAmounts;

	[Range(1, 100)]
	[FormerlySerializedAs("TransferAmount")]
	[FormerlySerializedAs("InitialTransferAmount")]
	[SerializeField] private float transferAmount = 20;

	public bool TraitWhitelistOn => traitWhitelist.Count > 0;

	public bool ReagentWhitelistOn => reagentWhitelist != null && reagentWhitelist.Count > 0;


	/// <summary>
	/// Server side only
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

		var useFillMessage = true;

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

}
