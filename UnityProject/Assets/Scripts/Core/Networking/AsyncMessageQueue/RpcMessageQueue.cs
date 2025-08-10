using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using Shared.Managers;

namespace Core.Networking.AsyncMessageQueue
{
	public class RpcMessageQueue : SingletonManager<RpcMessageQueue>
	{
		private Dictionary<string, QueuedMessage> _client_ReceivedMessages = new();
		private Dictionary<string, Func<string>> _server_RequestHandlers = new();

		public async UniTask<QueuedMessage> Queue(string requestedTicket, int minimumTimeoutTimeInMillaSecond = 25225)
		{
			var requestToken = Guid.NewGuid().ToString();
			var waitTick = 0;

			// Send the request using Net Messages
			RequestAsyncData.Send(requestedTicket, requestToken);

			while (waitTick < minimumTimeoutTimeInMillaSecond)
			{
				waitTick++;
				await UniTask.WaitForSeconds(0.05f);
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
				Requester = null,
				Status = MessageStatus.Failure,
				ValueFromJson = ""
			};
		}


		/// <summary>
		/// Register a handler function on the server side
		/// </summary>
		/// <param name="key">The key to identify this handler</param>
		/// <param name="handler">The function that returns a string value</param>
		public void RegisterHandler(string key, Func<string> handler)
		{
			_server_RequestHandlers[key] = handler;
		}

		/// <summary>
		/// Unregister a handler function on the server side
		/// </summary>
		/// <param name="key">The key of the handler to remove</param>
		public void UnregisterHandler(string key)
		{
			_server_RequestHandlers.Remove(key);
		}

		/// <summary>
		/// Process a response from the server on the client side
		/// </summary>
		/// <param name="token">The token that identifies this response</param>
		/// <param name="queuedMessage">The response message</param>
		public void ProcessResponse(string token, QueuedMessage queuedMessage)
		{
			if (_client_ReceivedMessages.Count > 100)
			{
				_client_ReceivedMessages.Clear();
			}

			// Store the result in the client's received messages dictionary using the token as key
			_client_ReceivedMessages[token] = queuedMessage;
		}

		/// <summary>
		/// Process a request from a client on the server side
		/// </summary>
		/// <param name="requestedTicket">The handler key to execute</param>
		/// <param name="token">Unique token for tracking this request</param>
		/// <param name="connection">The client connection that made the request</param>
		public void ProcessRequest(string requestedTicket, string token, NetworkConnection connection)
		{
			if (_server_RequestHandlers.TryGetValue(requestedTicket, out var func))
			{
				try
				{
					string resultValue = func.Invoke();
					var queuedMessage = new QueuedMessage
					{
						Requester = null,
						Status = MessageStatus.Success,
						ValueFromJson = resultValue
					};
					AsyncDataResponse.SendTo(connection, token, queuedMessage);
				}
				catch (Exception e)
				{
					var errorMessage = new QueuedMessage
					{
						Requester = null,
						Status = MessageStatus.Failure,
						ValueFromJson = ""
					};

					Loggy.Error(e.Message);
					AsyncDataResponse.SendTo(connection, token, errorMessage);
				}
			}
			else
			{
				var notFoundMessage = new QueuedMessage
				{
					Requester = null,
					Status = MessageStatus.Failure,
					ValueFromJson = ""
				};

				Loggy.Error($"Handler '{requestedTicket}' not found");
				AsyncDataResponse.SendTo(connection, token, notFoundMessage);
			}
		}
	}
}