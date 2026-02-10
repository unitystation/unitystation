using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Logs;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace US13.Systems.GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModeData")]
	public class GameModeData : ScriptableObject
	{
		/// <summary>
		/// All possible gamemodes
		/// </summary>
		[FormerlySerializedAs("GameModes")] [SerializeField]
		private List<GameMode> rotation = new();

		/// <summary>
		/// The default gamemode to pick
		/// </summary>
		[SerializeField]
		private GameMode DefaultGameMode;

		private List<GameMode> ShuffledList = new();

		[SerializeField][NotNull]
		private GameMode extendedReference;

		public GameMode ExtendedReference => extendedReference;

		private int shuffledListIndex = -1;

		/// <summary>
		/// Returns a list of game mode names available in the
		/// codebase
		/// </summary>
		public List<string> GetAvailableGameModeNames(bool allowExtended = false)
		{
			var gameModes = new List<string>();
			foreach (GameMode g in rotation)
			{
				gameModes.Add(g.Name);
			}

			if (allowExtended)
			{
				gameModes.Add("Extended");
			}

			return gameModes;
		}

		public GameMode GetGameMode(string gmName, bool allowExtended = false)
		{
			List<GameMode> list = new();
			list.AddRange(rotation);

			if (allowExtended)
			{
				list.Add(extendedReference);
			}

			foreach(GameMode gm in list)
			{
				if (gm.Name == gmName)
				{

					return Instantiate(gm);
				}
			}
			Loggy.Info($"Unable to get gamemode {gmName}, returning default: {DefaultGameMode.Name}", Category.GameMode);
			return GetDefaultGameMode();
		}

		/// <summary>
		/// Randomly chooses a gamemode that is possible with the current number of players
		/// </summary>
		public GameMode ChooseGameMode(bool allowExtended = false)
		{
			var possible = rotation.Where(gm => gm != null && gm.IsPossible()).ToList();

			if (allowExtended && extendedReference.IsPossible())
				possible.Add(extendedReference);

			return possible.Count == 0
				? GetDefaultGameMode()
				: Instantiate(possible.PickRandom());
		}

		public void IncrementCarouselIndex()
		{
			shuffledListIndex++;
			if (shuffledListIndex >= ShuffledList.Count)
			{
				shuffledListIndex = 0;
			}
		}

		public GameMode PickFromCarouselGameMode(bool allowExtended = false)
		{
			RefillCarouselIfNeeded(allowExtended);

			int count = ShuffledList.Count;
			if (count == 0)
				return GetDefaultGameMode();

			for (int tries = 0; tries < count; tries++)
			{
				IncrementCarouselIndex();
				GameMode gm = ShuffledList[shuffledListIndex];

				if (gm != null && gm.IsPossible())
					return Instantiate(gm);
			}

			IncrementCarouselIndex();
			return GetDefaultGameMode();
		}

		private void RefillCarouselIfNeeded(bool allowExtended)
		{
			if (ShuffledList.Count != 0)
				return;

			ShuffledList.Clear();
			ShuffledList.AddRange(rotation);

			if (allowExtended && extendedReference != null)
				ShuffledList.Add(extendedReference);

			for (int i = ShuffledList.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				(ShuffledList[i], ShuffledList[j]) = (ShuffledList[j], ShuffledList[i]);
			}
		}

		/// <summary>
		/// Returns the default game mode
		/// </summary>
		public GameMode GetDefaultGameMode()
		{
			return Instantiate(DefaultGameMode);
		}

	}
}
