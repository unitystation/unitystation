using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;

namespace Core.Networking.AsyncMessageQueue
{
	public class RpcMessageQueue : NetworkBehaviour
	{
		public static RpcMessageQueue Instance;
		private Dictionary<string, QueuedMessage> _client_ReceivedMessages = new();
		public Dictionary<string, Func<string>> ServerRequestHandlers = new();

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		[TargetRpc]
		private void SendResultToClient(NetworkConnectionToClient target, QueuedMessage result, string token)
		{
			if (_client_ReceivedMessages.Count > 100)
			{
				_client_ReceivedMessages.Clear();
			}
			// Store the result in the client's received messages dictionary using the token as key
			_client_ReceivedMessages[token] = result;
		}

		[Command(requiresAuthority = false)]
		private void CmdQueueMessage(string requestedTicket, NetworkIdentity requester, string token)
		{
			if (ServerRequestHandlers.TryGetValue(requestedTicket, out var func))
			{
				try
				{
					string resultValue = func.Invoke();
					var queuedMessage = new QueuedMessage
					{
						Requester = requester,
						Status = MessageStatus.Success,
						ValueFromJson = resultValue
					};

					if (requester != null && requester.connectionToClient != null)
					{
						SendResultToClient(requester.connectionToClient, queuedMessage, token);
					}
				}
				catch (Exception e)
				{
					var errorMessage = new QueuedMessage
					{
						Requester = requester,
						Status = MessageStatus.Failure,
						ValueFromJson = ""
					};

					Loggy.Error(e.Message);
					if (requester != null && requester.connectionToClient != null)
					{
						SendResultToClient(requester.connectionToClient, errorMessage, token);
					}
				}
			}
			else
			{
				var notFoundMessage = new QueuedMessage
				{
					Requester = requester,
					Status = MessageStatus.Failure,
					ValueFromJson = ""
				};

				Loggy.Error($"Handler '{requestedTicket}' not found");
				if (requester != null && requester.connectionToClient != null)
				{
					SendResultToClient(requester.connectionToClient, notFoundMessage, token);
				}
			}
		}

		public async UniTask<QueuedMessage> Queue(string requestedTicket, NetworkIdentity requester, int minimumTimeoutTimeInSeconds = 25)
		{
			var requestToken = Guid.NewGuid().ToString();
			var waitTick = 0;
			CmdQueueMessage(requestedTicket, requester, requestToken);
			while (waitTick < minimumTimeoutTimeInSeconds)
			{
				waitTick++;
				await UniTask.WaitForSeconds(1);
				if (_client_ReceivedMessages.ContainsKey(requestToken))
				{
					var result = _client_ReceivedMessages[requestToken];
					_client_ReceivedMessages.Remove(requestToken); // Clean up after retrieving
					return result;
				}
			}

			Loggy.Error($"Request timed out for {requestedTicket} - {requestToken}");
			return new QueuedMessage
			{
				Requester = requester,
				Status = MessageStatus.Failure,
				ValueFromJson = ""
			};
		}
	}
}