using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEngine;

public class SpeciesChoiceElement : DynamicEntry
{
	public PlayerHealthData Species;
	public SudokuPuzzleGame SudokuPuzzleGame;


	public void SetValues(PlayerHealthData InSpecies, SudokuPuzzleGame InSudokuPuzzleGame)
	{
		Species = InSpecies;
		SudokuPuzzleGame = InSudokuPuzzleGame;
	}

	public void OnSelect()
	{
		SudokuPuzzleGame.GenerateForSpecies(Species);
	}
}
