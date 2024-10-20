using System;
using System.Collections.Generic;
using AddressableReferences;
using Items.Others;
using Mirror;
using UnityEngine;

namespace Items.Bureaucracy
{
	public class BookWritable : NetworkBehaviour, ICheckedInteractable<HandActivate>
	{
		[SerializeField] private ItemAttributesV2 attributes;
		[SerializeField] private ItemStorage paperStorage;
		[SerializeField] private HasNetworkTabItem tab;
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private List<AddressableAudioSource> pageTurnSounds = new List<AddressableAudioSource>();
		[field: SerializeField, SyncVar] public List<BookPage> Pages { get; private set; } = new List<BookPage>();

		private void Awake()
		{
			tab ??= GetComponent<HasNetworkTabItem>();
			attributes ??= GetComponent<ItemAttributesV2>();
			paperStorage ??= GetComponent<ItemStorage>();
			spriteHandler ??= GetComponentInChildren<SpriteHandler>();
		}

		private void Start()
		{
			if (CustomNetworkManager.IsServer && paperStorage.HasAnyOccupied())
			{
				Setup(paperStorage.GetItemsWithComponent<Paper>(), attributes.ArticleName, attributes.ArticleDescription);
			}
		}

		[Server]
		public void Setup(List<Paper> papers, string bookTitle, string bookDesc)
		{
			if (papers == null || papers.Count == 0) return;
			var pageNumber = -1;
			foreach (var paper in papers)
			{
				pageNumber++;
				BookPage newPage = new BookPage()
				{
					PageContent = paper.ServerString,
					PageNumber = pageNumber
				};
				Pages.Add(newPage);
			}
			attributes.ServerSetArticleName(bookTitle);
			attributes.ServerSetArticleDescription(bookDesc);
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			var progress = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Escape), () =>
			{
				if (Pages.Count == 0)
				{
					Chat.AddLocalMsgToChat($"The '{gameObject.ExpensiveName()}' crumbles and turns to dust..", gameObject);
					_ = Despawn.ServerSingle(gameObject);
					return;
				}
				tab.ServerPerformInteraction(interaction);
				PlayPageTurnSound();
			});
			progress.ServerStartProgress(gameObject.AssumedWorldPosServer(), 0.25f,
				interaction.PerformerPlayerScript.gameObject);
		}

		public void PlayPageTurnSound()
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(pageTurnSounds.PickRandom(), gameObject.AssumedWorldPosServer());
		}
	}

	[Serializable]
	public class BookPage
	{
		public string PageContent;
		public int PageNumber;
	}
}