# RPC Message Queue System

This system provides an asynchronous message queue for client-server communication in Unity using Mirror networking.

## Overview

The `RpcMessageQueue` system allows clients to request data from the server and receive responses asynchronously. The server executes registered functions that return text values, which are then wrapped into `QueuedMessage` objects and sent back to the requesting client.

## Key Features

- **Asynchronous Communication**: Clients can request data without blocking
- **Function Registration**: Server can register functions that return text values
- **Error Handling**: Comprehensive error handling with meaningful error messages
- **Type Safety**: Built-in deserialization support for various data types
- **Token-based Tracking**: Each request gets a unique token for tracking responses

## System Components

### RpcMessageQueue
The main class that handles the message queue system.

**Key Properties:**
- `ServerRequestHandlers`: Dictionary of registered functions that return strings
- `_client_ReceivedMessages`: Dictionary storing received messages by token

**Key Methods:**
- `Queue()`: Request data from the server asynchronously
- `CmdQueueMessage()`: Server-side command to process requests
- `SendResultToClient()`: Send results back to clients

### QueuedMessage
Represents a message with status and value information.

**Properties:**
- `ValueFromJson`: The text value returned by the server function
- `Requester`: The NetworkIdentity that made the request
- `Status`: Success or Failure status

**Methods:**
- `DeserializeFromText<T>()`: Deserialize the text value to a specific type

## Usage

### 1. Register Server Handlers

On the server, register functions that return string values:

```csharp
// Register a simple text handler
RpcMessageQueue.Instance.ServerRequestHandlers["GetPlayerCount"] = () =>
{
    return PlayerManager.Instance.GetPlayerCount().ToString();
};

// Register a JSON handler
RpcMessageQueue.Instance.ServerRequestHandlers["GetServerInfo"] = () =>
{
    var serverInfo = new { name = "Server", version = "1.0" };
    return JsonUtility.ToJson(serverInfo);
};
```

### 2. Request Data from Client

On the client, request data asynchronously:

```csharp
public async void RequestPlayerCount()
{
    var result = await RpcMessageQueue.Instance.Queue("GetPlayerCount", GetComponent<NetworkIdentity>());
    
    if (result.Status == MessageStatus.Success)
    {
        int playerCount = result.DeserializeFromText<int>();
        Debug.Log($"Player count: {playerCount}");
    }
    else
    {
        Debug.LogError($"Failed: {result.ValueFromJson}");
    }
}
```

### 3. Handle Complex Data

For complex data structures, use JSON serialization:

```csharp
// Server handler
RpcMessageQueue.Instance.ServerRequestHandlers["GetPlayerData"] = () =>
{
    var playerData = new PlayerData { name = "Player1", score = 100 };
    return JsonUtility.ToJson(playerData);
};

// Client usage
var result = await RpcMessageQueue.Instance.Queue("GetPlayerData", GetComponent<NetworkIdentity>());
if (result.Status == MessageStatus.Success)
{
    var playerData = result.DeserializeFromText<PlayerData>();
    Debug.Log($"Player: {playerData.name}, Score: {playerData.score}");
}
```

## Supported Data Types

The `DeserializeFromText<T>()` method supports:
- Primitive types: `int`, `float`, `double`, `bool`, `decimal`, `long`, `short`, `byte`
- `string` (returns the raw text)
- Complex objects (via JSON deserialization)

## Error Handling

The system provides comprehensive error handling:

1. **Handler Not Found**: Returns failure status with "Handler not found" message
2. **Execution Errors**: Catches exceptions and returns error details
3. **Timeout**: Returns failure status if no response within 25 seconds
4. **Network Issues**: Handles null connections gracefully

## Example Implementation

See `RpcMessageQueueExample.cs` for a complete example of how to use the system.

## Best Practices

1. **Always unregister your Funcs on Disable/Destroy**: Register all handlers in `OnDisable()` or `OnDestroy()`
2. **Use meaningful keys**: Use descriptive names for handler keys
3. **Handle errors**: Always check the `Status` before processing results
4. **Clean up**: The system automatically cleans up messages after retrieval
5. **Use appropriate data types**: Choose the right serialization method for your data


# Full Example

```cs

using UnityEngine;
using Core.Networking.AsyncMessageQueue;

namespace Core.Networking.AsyncMessageQueue.Examples
{
	/// <summary>
	/// Example class demonstrating how to use the updated RpcMessageQueue system
	/// with Func<string> handlers that return text values.
	/// </summary>
	public class RpcMessageQueueExample : MonoBehaviour
	{
		private void Start()
		{
			// Register handlers that return text values
			RegisterExampleHandlers();
		}

		private void RegisterExampleHandlers()
		{
			if (RpcMessageQueue.Instance == null) return;

			// Register a simple text handler
			RpcMessageQueue.Instance.ServerRequestHandlers["GetPlayerCount"] = () =>
			{
				return UnityEngine.Random.Range(1, 100).ToString();
			};

			// Register a handler that returns JSON data
			RpcMessageQueue.Instance.ServerRequestHandlers["GetServerInfo"] = () =>
			{
				var serverInfo = new
				{
					serverName = "UnityStation",
					version = "1.0.0",
					uptime = Time.time,
					playerCount = UnityEngine.Random.Range(1, 50)
				};

				return JsonUtility.ToJson(serverInfo);
			};

			// Register a handler that returns a simple message
			RpcMessageQueue.Instance.ServerRequestHandlers["GetWelcomeMessage"] = () =>
			{
				return "Welcome to UnityStation!";
			};

			// Register a handler that performs some calculation
			RpcMessageQueue.Instance.ServerRequestHandlers["CalculateScore"] = () =>
			{
				int score = UnityEngine.Random.Range(100, 1000);
				return score.ToString();
			};
		}

		/// <summary>
		/// Example method showing how to request data from the server
		/// </summary>
		public async void RequestPlayerCount()
		{
			if (RpcMessageQueue.Instance == null) return;

			var result = await RpcMessageQueue.Instance.Queue("GetPlayerCount", GetComponent<NetworkIdentity>());

			if (result.Status == MessageStatus.Success)
			{
				Debug.Log($"Player count: {result.ValueFromJson}");
			}
			else
			{
				Debug.LogError($"Failed to get player count: {result.ValueFromJson}");
			}
		}

		/// <summary>
		/// Example method showing how to request and deserialize JSON data
		/// </summary>
		public async void RequestServerInfo()
		{
			if (RpcMessageQueue.Instance == null) return;

			var result = await RpcMessageQueue.Instance.Queue("GetServerInfo", GetComponent<NetworkIdentity>());

			if (result.Status == MessageStatus.Success)
			{
				// The QueuedMessage class has a DeserializeFromText method for JSON
				var serverInfo = result.DeserializeFromText<ServerInfo>();
				Debug.Log($"Server: {serverInfo.serverName}, Version: {serverInfo.version}");
			}
			else
			{
				Debug.LogError($"Failed to get server info: {result.ValueFromJson}");
			}
		}

		/// <summary>
		/// Example method showing how to request a simple text message
		/// </summary>
		public async void RequestWelcomeMessage()
		{
			if (RpcMessageQueue.Instance == null) return;

			var result = await RpcMessageQueue.Instance.Queue("GetWelcomeMessage", GetComponent<NetworkIdentity>());

			if (result.Status == MessageStatus.Success)
			{
				Debug.Log($"Message: {result.ValueFromJson}");
			}
			else
			{
				Debug.LogError($"Failed to get welcome message: {result.ValueFromJson}");
			}
		}

		/// <summary>
		/// Example method showing how to request and parse numeric data
		/// </summary>
		public async void RequestScore()
		{
			if (RpcMessageQueue.Instance == null) return;

			var result = await RpcMessageQueue.Instance.Queue("CalculateScore", GetComponent<NetworkIdentity>());

			if (result.Status == MessageStatus.Success)
			{
				// Parse the score as an integer
				int score = result.DeserializeFromText<int>();
				Debug.Log($"Calculated score: {score}");
			}
			else
			{
				Debug.LogError($"Failed to calculate score: {result.ValueFromJson}");
			}
		}
	}

	/// <summary>
	/// Example data structure for JSON deserialization
	/// </summary>
	[System.Serializable]
	public class ServerInfo
	{
		public string serverName;
		public string version;
		public float uptime;
		public int playerCount;
	}
}
```