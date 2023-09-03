using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Messages.Server;
using UnityEngine.Events;

namespace UI.Core.NetUI
{
	/// <summary>
	/// Page switcher script.
	/// If pages aren't defined manually,
	/// they are scanned among immediate children
	/// of this gameObject (non-recursive)
	/// </summary>
	public class NetPageSwitcher : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		public List<NetPage> Pages = new List<NetPage>();
		public NetPage DefaultPage;
		[NonSerialized]
		public NetPage CurrentPage;
		private int CurrentPageIndex => Pages.IndexOf(CurrentPage);
		public bool StartInitialized { get; private set; } = false;

		/// <summary>
		/// Serverside event, hook up with it to do custom things on page change
		/// </summary>
		public PageChangeEvent OnPageChange;

		public override void ExecuteServer(PlayerInfo subject) { }

		public override string Value {
			get => CurrentPageIndex.ToString();
			protected set {
				externalChange = true;

				if (int.TryParse(value, out var parsedValue) && Pages.Count > parsedValue && parsedValue > -1)
				{
					SetActivePageInternal(Pages[parsedValue]);
				}
				else
				{
					Loggy.LogErrorFormat("'{0}' page switcher: unknown index value {1}", Category.NetUI, gameObject.name, value);
				}
				externalChange = false;
			}
		}

		public override void Init()
		{
			if (Pages.Count == 0)
			{
				Pages = this.GetComponentsOnlyInChildren<NetPage>().ToList();
				Loggy.LogFormat("'{0}' page switcher: dev didn't add any pages to the list, found {1} page(s)",
					Category.NetUI, gameObject.name, Pages.Count);
			}

			if (Pages.Count > 0)
			{
				if (!DefaultPage)
				{
					DefaultPage = Pages[0];
					Loggy.LogFormat("'{0}' page switcher: Default Page not set explicitly, assuming it's {1}", Category.NetUI,
						gameObject.name, DefaultPage);
				}
			}

			if (containedInTab.IsMasterTab && StartInitialized == false)
			{
				//Enabling all pages
				//so that all elements will be visible during Start()
				foreach (var page in Pages)
				{
					page.gameObject.SetActive(true);
				}
				StartInitialized = true;
			}
		}

		public override void AfterInit()
		{
			if (containedInTab.IsMasterTab && DefaultPage && CurrentPage == false)
			{
				SetActivePage(DefaultPage);
			}
		}

		///Not just own value, include current page elements as well
		protected override void UpdatePeepersLogic()
		{
			List<ElementValue> valuesToSend = new List<ElementValue>(100) { ElementValue };
			foreach (NetUIElementBase entry in CurrentPage.Elements)
			{
				valuesToSend.Add(entry.ElementValue);
			}

			TabUpdateMessage.SendToPeepers(containedInTab.Provider, containedInTab.Type, TabAction.Update, valuesToSend.ToArray());
		}

		/// <summary>
		///	[Server]
		/// Activate desired page
		/// </summary>
		public void SetActivePage(NetPage page)
		{
			MasterSetValue(Pages.IndexOf(page).ToString());
		}

		/// <summary>
		/// [Server]
		/// Activates the page corresponding to the given page index.
		/// </summary>
		/// <param name="pageIndex">The index of the page to be activated (from Pages field)</param>
		public void SetActivePage(int pageIndex)
		{
			if (Pages.ElementAtOrDefault(pageIndex) == null) return;

			SetActivePage(Pages[pageIndex]);
		}

		private void SetActivePageInternal(NetPage newPage)
		{
			if (!newPage)
			{
				Loggy.LogErrorFormat("'{0}' page switcher: trying to activate null page", Category.NetUI, gameObject.name);
				return;
			}

			foreach (var listedPage in Pages)
			{
				if (listedPage != newPage)
				{
					listedPage.gameObject.SetActive(false);
				}
			}

			Loggy.LogTraceFormat("'{0}' page switcher: activating page {1}", Category.NetUI, gameObject.name, newPage);

			newPage.gameObject.SetActive(true);

			if (containedInTab.IsMasterTab)
			{
				OnPageChange.Invoke(CurrentPage, newPage);
			}

			CurrentPage = newPage;

			containedInTab.RescanElements();
		}

		/// <summary>
		///	[Server]
		/// Tries to go to next page from Pages list
		/// </summary>
		/// <param name="wrap">Set to true if you want infinite scrolling</param>
		public void NextPage(bool wrap = false)
		{
			int pageCount = Pages.Count;
			int suggestedIndex = CurrentPageIndex + 1;
			if (wrap)
			{
				MasterSetValue(Pages.WrappedIndex(suggestedIndex).ToString());
			}
			else
			{
				if (suggestedIndex >= pageCount)
				{
					Loggy.LogTraceFormat("'{0}' page switcher: no more >> pages to switch to (index={1})", Category.NetUI, gameObject.name, suggestedIndex);
					return;
				}
				MasterSetValue(suggestedIndex.ToString());
			}
		}

		/// <summary>
		///	[Server]
		/// Tries to go to previous page from Pages list
		/// </summary>
		/// <param name="wrap">Set to true if you want infinite scrolling</param>
		public void PreviousPage(bool wrap = false)
		{
			int suggestedIndex = CurrentPageIndex - 1;
			if (wrap)
			{
				MasterSetValue(Pages.WrappedIndex(suggestedIndex).ToString());
			}
			else
			{
				if (suggestedIndex < 0)
				{
					Loggy.LogTraceFormat("'{0}' page switcher: no more << pages to switch to (index={1})", Category.NetUI, gameObject.name, suggestedIndex);
					return;
				}
				MasterSetValue(suggestedIndex.ToString());
			}
		}
	}

	[Serializable]
	public class PageChangeEvent : UnityEvent<NetPage, NetPage> { }
}
