using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;

public class SudokuPuzzleGame : MonoBehaviour
{

	//TODO At countdown timer for check validity
	//TODO Add unlock, Set up with Species stuff

	public NetText_label Status;

	public NetCountdownTimer NetCountdownTimer;

	public List<NetTMPInputField> NetText_labels = new List<NetTMPInputField>();

	public GUI_DNAConsole GUI_DNAConsole;

	public EmptyItemList SpeciesSelectionList;

	public PlayerHealthData currentlySelected;
	public BodyPartMutations.MutationRoundData currentlySelectedData;

	public Dictionary<PlayerHealthData, SpeciesChoiceElement> MutationToElement =
		new Dictionary<PlayerHealthData, SpeciesChoiceElement>();
	public void Start()
	{
		if (GUI_DNAConsole.IsMasterTab)
		{
			foreach (var Species in GUI_DNAConsole.DNAConsole.ALLSpecies)
			{
				var Element =  SpeciesSelectionList.AddItem() as SpeciesChoiceElement;
				Element.SetValues(Species, this);
				MutationToElement[Species] = Element;
			}

		}
	}


	public void GenerateForSpecies(PlayerHealthData PlayerHealthData)
	{
		currentlySelected = PlayerHealthData;
		currentlySelectedData = BodyPartMutations.GetSpeciesRoundData(currentlySelected);
		PopulatePuzzle();
	}

	public void PopulatePuzzle()
	{
		ResetBoard();
		Status.SetValue("Puzzle Ready");
	}

	public void clearBoard()
	{
		for (int i = 0; i < NetText_labels.Count; i++)
		{
			NetText_labels[i].SetValue("");
		}
	}

	public void ResetBoard()
	{
		if (currentlySelectedData == null)
		{
			Status.SetValue("nothing selected");
			return;
		}
		for (int i = 0; i < NetText_labels.Count; i++)
		{
			var toSet = currentlySelectedData.SudokuPuzzle[i];
			if (toSet == '.')
			{
				NetText_labels[i].SetValue("");
			}
			else
			{
				NetText_labels[i].SetValue(toSet.ToString());
			}
		}
		Status.SetValue("Puzzle Refreshed");
	}


	public void UserValidatePuzzle()
	{
		if (NetCountdownTimer.Completed == false) return;
		ValidatePuzzle(true);
		NetCountdownTimer.StartCountdown(15);
	}

	public bool ValidatePuzzle(bool Show = false)
	{
		if (currentlySelectedData == null)
		{
			if (Show)
			{
				Status.SetValue("nothing selected");
			}

			return false;
		}

		//Reapply, so they can't just change it to a valid easy one
		for (int i = 0; i < NetText_labels.Count; i++)
		{
			var toSet = currentlySelectedData.SudokuPuzzle[i];
			if (toSet == '.')
			{

			}
			else
			{
				NetText_labels[i].SetValue(toSet.ToString());
			}
		}


		var board = "";
		for (int i = 0; i < NetText_labels.Count; i++)
		{
			if (string.IsNullOrEmpty(NetText_labels[i].Value))
			{
				board += ".";
			}
			else
			{
				board += NetText_labels[i].Value;
			}

		}

		var SGen = new SudokuGenerator();
		var Solve = SGen.solve(board);
		if (string.IsNullOrEmpty(Solve))
		{
			if (Show)
			{
				Status.SetValue("Invalid board incorrect state");
			}

			return false;
		}
		else
		{
			if (board.Contains("."))
			{
				if (Show)
				{
					Status.SetValue("Board is valid but not completed");
				}

				return false;
			}
			else
			{
				if (Show)
				{
					Status.SetValue("Board is valid and Completed yay");
				}

				return true;
			}
		}
	}

	public void TryUnlockPuzzle()
	{
		if (currentlySelectedData == null)
		{
			Status.SetValue("nothing selected");
			return;
		}

		if (ValidatePuzzle())
		{
			GUI_DNAConsole.AddSpecies(currentlySelected);
			SpeciesSelectionList.MasterRemoveItem(MutationToElement[currentlySelected]);
			MutationToElement.Remove(currentlySelected);
			currentlySelected = null;
			currentlySelectedData = null;
			clearBoard();

		}
	}
}
