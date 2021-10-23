using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using AddressableReferences;


namespace Items.Bureaucracy
{
	/// <summary>
	/// Allows players to roleplay reading books.
	/// </summary>
	public class SimpleBook : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		[Tooltip("How many pages (or remarks) to read before this book is considered read.")]
		[BoxGroup("Settings"), SerializeField, Range(1, 10)]
		private int pagesToRead = 3;
		[Tooltip("How long each page (or remark) takes to read.")]
		[BoxGroup("Settings"), SerializeField]
		private float timeToReadPage = 5f;
		[Tooltip("Whether this book can be read by the same person multiple times.")]
		[BoxGroup("Settings"), SerializeField]
		private bool canBeReadMultipleTimes = true;
		[Tooltip("Whether only one person (the first to attempt) can read this book.")]
		[BoxGroup("Settings"), SerializeField]
		private bool allowOnlyOneReader = false;

		[Tooltip("The possible strings that could be chosen to display to the reader when a page is considered read.")]
		[SerializeField, ReorderableList]
		private string[] remarks = default;

		[SerializeField]
		private List<AddressableAudioSource> pageturnSfx = default;

		private readonly Dictionary<ConnectedPlayer, int> readerProgress = new Dictionary<ConnectedPlayer, int>();
		protected bool hasBeenRead = false;

		protected bool AllowOnlyOneReader => allowOnlyOneReader;

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			ConnectedPlayer player = interaction.Performer.Player();

			if (TryReading(player))
			{
				StartReading(player);
			}
		}

		/// <summary>
		/// Whether it is possible for the reader to read this book.
		/// </summary>
		/// <returns></returns>
		protected virtual bool TryReading(ConnectedPlayer player)
		{
			if (canBeReadMultipleTimes == false &&
					readerProgress.ContainsKey(player) && readerProgress[player] > pagesToRead)
			{
				Chat.AddExamineMsgFromServer(player.GameObject, $"You already know all about <b>{gameObject.ExpensiveName()}</b>!");
				return false;
			}
			if (AllowOnlyOneReader && hasBeenRead)
			{
				Chat.AddExamineMsgFromServer(player.GameObject, $"It seems you can't read this book... has someone claimed it?");
				return false;
			}

			return true;
		}

		private void StartReading(ConnectedPlayer player)
		{
			if (readerProgress.ContainsKey(player) == false)
			{
				readerProgress.Add(player, 0);
				Chat.AddActionMsgToChat(player.GameObject,
						$"You begin reading {gameObject.ExpensiveName()}...",
						$"{player.Script.visibleName} begins reading {gameObject.ExpensiveName()}...");
				ReadBook(player);
			}
			else
			{
				Chat.AddActionMsgToChat(player.GameObject,
						$"You resume reading {gameObject.ExpensiveName()}...",
						$"{player.Script.visibleName} resumes reading {gameObject.ExpensiveName()}...");
				ReadBook(player, readerProgress[player]);
			}
		}

		// Note: this is a recursive method.
		private void ReadBook(ConnectedPlayer player, int pageToRead = 0)
		{
			var playerTile = player.GameObject.RegisterTile();
			if (pageToRead >= pagesToRead || pageToRead > 10)
			{
				FinishReading(player);
				return;
			}

			StandardProgressActionConfig cfg = new StandardProgressActionConfig(
				StandardProgressActionType.Construction,
				false,
				false
			);
			StandardProgressAction.Create(cfg, ReadPage).ServerStartProgress(
				playerTile,
				timeToReadPage,
				player.GameObject
			);

			void ReadPage()
			{
				readerProgress[player]++;

				SoundManager.PlayNetworkedAtPos(pageturnSfx.PickRandom(), playerTile.WorldPositionServer, sourceObj: player.GameObject);
				Chat.AddExamineMsgFromServer(player.GameObject, remarks.PickRandom());

				ReadBook(player, readerProgress[player]);
			}
		}

		/// <summary>
		/// Triggered when the reader has read all of the pages.
		/// </summary>
		protected virtual void FinishReading(ConnectedPlayer player)
		{
			hasBeenRead = true;

			if (canBeReadMultipleTimes)
			{
				readerProgress[player] = 0;
			}

			Chat.AddActionMsgToChat(player.GameObject,
					$"You finish reading {gameObject.ExpensiveName()}!",
					$"{player.Script.visibleName} finishes reading {gameObject.ExpensiveName()}!");
		}
	}
}
