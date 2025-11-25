using System;
using System.Collections.Generic;
using Core.Editor.Attributes;
using Core.Networking.AsyncMessageQueue;
using Logs;
using Managers.Supporters.FetchSupporterMethods;
using Newtonsoft.Json;
using Shared.Managers;
using UnityEngine;

namespace Managers.Supporters
{
	public class Supporters : SingletonManager<Supporters>
	{
		[SerializeReference, SelectImplementation(typeof(IFetchSupporters))]
		public List<IFetchSupporters> FetchMethods = new();

		[HideInInspector]
		public List<Supporter> SupporterList = new();

		public override void Awake()
		{
			base.Awake();
			RefreshSupporters();
			RpcMessageQueue.Instance.RegisterHandler(RequestHandlerConstants.REQUEST_SUPPORTERS, CurrentSupportersToJson);
		}

		public void RefreshSupporters()
		{
			SupporterList.Clear();
			foreach (var method in FetchMethods)
			{
				try
				{
					SupporterList.AddRange(method.FetchSupporters());
				}
				catch (Exception e)
				{
					Loggy.Error($"An error occurred while fetching supporters: {e.Message}");
				}
			}
		}

		private string CurrentSupportersToJson()
		{
			return JsonConvert.SerializeObject(SupporterList);
		}

		public static Tuple<bool, Supporter?> IsSupporter(PlayerInfo player)
		{
			foreach (var supporter in Instance.SupporterList)
			{
#if UNITY_EDITOR
				if (player.Username == supporter.Identifier) return new Tuple<bool, Supporter?>(true, supporter);
#endif
				if (player.AccountId == supporter.Identifier) return new Tuple<bool, Supporter?>(true, supporter);
			}
			return new Tuple<bool, Supporter?>(false, null);
		}
	}

	public struct Supporter
	{
		public string Identifier;
	}
}

