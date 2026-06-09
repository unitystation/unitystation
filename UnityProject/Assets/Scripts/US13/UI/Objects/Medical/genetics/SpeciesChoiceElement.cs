using US13.Player;
using US13.UI.Core.Net.Elements.Dynamic;
using US13.UI.Objects.Medical.genetics.SudokuPuzzle;

namespace US13.UI.Objects.Medical.genetics
{
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
}
