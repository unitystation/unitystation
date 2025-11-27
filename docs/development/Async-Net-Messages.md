# How to use `RpcMessageQueue` (Async Net Messages)

The `RpcMessageQueue` class provides a simple, asynchronous way for clients to request data from the server and get a typed response back, without needing to write a custom `NetMessage` implementation for every request type.

It functions similarly to a promise-based RPC system, using unique request tokens to match responses to their original requests.

This system is ideal for:

- Fetching server-only game state (e.g., round state, player data, configuration)
- Performing simple remote queries
- Returning structured data to the client
- You hate writing long boilerplate code every time you want to perform basic networking things.

If you want data and actions to be synced across all clients, please use Mirror's native features such as the `[SyncVar]`, and `[ClientRpc]` attributes instead of this.

## Registering Actions on the server

The server must register an action with the `RpcMessageQueue` with a ticket that is associated with the action, or the client will receive an error indicating that the action is not registered. (Check the errors section for more information on errors)

```csharp
public void RegisterHandler(string key, Func<string> handler)
```
Example:
```csharp
RpcMessageQueue.Instance.RegisterHandler(RequestHandlerConstants.REQUEST_ROUND_STATUS, CurrentRoundStatusToJson);
```

For clarity and error-prevention, we keep all ticket string ids as constants in the `RequestHandlerConstants` class.

Registering a handle can only be done on a server, as the client always asks the server to check its side of the `_server_RequestHandlers` dictionary.

You can return any kind of string values (including empty ones) via the registered handler. The client will have to decode the contents of the string (or ignore it if it's not needed) on their end once they receive the response from the queued message that they called.

Remember to unregister when the object that's associated with it is being destroyed using `UnregisterHandler(string key)`.

## Calling Actions

All actions are queued via the `RpcMessageQueue.Instance.Queue` method on clients. This method returns a `UniTask<QueuedMessage>` which is to be awaited for the results from the server.

By default, all queued messages have 25 seconds before timing out. This can be changed by changing the default argument of the queue function.

```csharp
public async UniTask<QueuedMessage> Queue(string requestedTicket, int minimumTimeoutTimeInMillaSecond = 25225)
```

Example:

```csharp
private async UniTask AskServerDirectlyForRoundState()
{
	QueuedMessage status = await RpcMessageQueue.Instance.Queue(RequestHandlerConstants.REQUEST_ROUND_STATUS);
	Loggy.Info($"Status: {status.Status}\n data: {status.ValueFromJson}", Category.Networking);
	switch(status.Status)
	{
		case MessageStatus.Success:
			var state = status.DeserializeFromText<RoundState>();
			// do whatever you want here now with the state.
			break;
		case MessageStatus.Failure:
			Loggy.Error($"{status.ValueFromJson}"); // Errors will be placed in the value propriety when there's no actual value.
			break;
	}
}
```

Each `QueuedMessage` comes with a `DeserializeFromText<T>()` method that can automatically interpret any values sent from the server to the correct type. This is mainly used for deserializing JSON data into C# objects, but it can be used for:

- `int`
- `float`
- `double`
- `decimal`
- `long`
- `short`
- `byte`
- `bool`

You could manually just do the conversion yourself, but why should you when the work is already done for you? Just use the `DeserializeFromText<T>()` method unless you're dealing with unsupported types from this method, or your data format is not in JSON and is in something else, like TOML.


## Error Codes

| Error Code | Meaning | Trigger Condition |
|-----------|---------|-------------------|
| **E.0** | **Unexpected server-side exception** | Thrown when a registered handler executes but throws an exception. The exception details are included in the response. |
| **E.1** | **Missing request handler** | Occurs when the client requests a handler key that is not registered on the server (`_server_RequestHandlers` does not contain the key). |
| **E.3** | **Request timeout** | The client waited longer than the configured timeout (`minimumTimeoutTimeInMillaSecond`) without receiving a server response. |

## Important Notes & Limitations

- Tokens are removed after being retrieved.
- CPU cost is low, but still causes continuous polling. (interval: 0.05s)
- This system is built around `UniTask` in mind. You may run into some quirks while using `Task`, or if you operate outside of the main thread.
