using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Logs;
using Mirror;
using SecureStuff;
using UnityEngine;
using US13.Managers;

namespace IngameDebugConsole.Scripts
{
	/// <summary>
	/// Helper class to store important information about a command
	/// </summary>
	/// <remarks>
	/// Manages the console commands, parses console input and handles execution of commands
	/// Supported method parameter types: int, float, bool, string, Vector2, Vector3, Vector4
	/// </remarks>
	public class ConsoleMethodInfo
	{
		public readonly MethodInfo method;
		public readonly Type[] parameterTypes;
		public readonly object instance;

		public readonly string signature;

		public ConsoleMethodInfo(MethodInfo method, Type[] parameterTypes, object instance, string signature)
		{
			this.method = method;
			this.parameterTypes = parameterTypes;
			this.instance = instance;
			this.signature = signature;
		}

		public bool IsValid()
		{
			if (method.IsStatic == false && (instance == null || instance.Equals(null)))
				return false;

			return true;
		}
	}

	/// <summary>
	/// Manages the console commands, parses console input and handles execution of commands
	/// </summary>
	public static class DebugLogConsole
	{
		public delegate bool ParseFunction(string input, out object output);

		/// <summary>
		/// All the commands
		/// </summary>
		public static Dictionary<string, ConsoleMethodInfo> RegisteredMethodInfos { get; private set; }= new();

		/// <summary>
		/// All the parse functions
		/// </summary>
		private static Dictionary<Type, ParseFunction> parseFunctions = new Dictionary<Type, ParseFunction>()
		{
			{typeof(string), ParseString},
			{typeof(bool), ParseBool},
			{typeof(int), ParseInt},
			{typeof(uint), ParseUInt},
			{typeof(long), ParseLong},
			{typeof(ulong), ParseULong},
			{typeof(byte), ParseByte},
			{typeof(sbyte), ParseSByte},
			{typeof(short), ParseShort},
			{typeof(ushort), ParseUShort},
			{typeof(char), ParseChar},
			{typeof(float), ParseFloat},
			{typeof(double), ParseDouble},
			{typeof(decimal), ParseDecimal},
			{typeof(Vector2), ParseVector2},
			{typeof(Vector3), ParseVector3},
			{typeof(Vector4), ParseVector4},
			{typeof(GameObject), ParseGameObject},
			{typeof(NetworkConnectionToClient), ParseNetworkConnectionToClient}
		};

		/// <summary>
		/// All the readable names of accepted types
		/// </summary>
		private static Dictionary<Type, string> typeReadableNames = new Dictionary<Type, string>()
		{
			{typeof(string), "String"},
			{typeof(bool), "Boolean"},
			{typeof(int), "Integer"},
			{typeof(uint), "Unsigned Integer"},
			{typeof(long), "Long"},
			{typeof(ulong), "Unsigned Long"},
			{typeof(byte), "Byte"},
			{typeof(sbyte), "Short Byte"},
			{typeof(short), "Short"},
			{typeof(ushort), "Unsigned Short"},
			{typeof(char), "Char"},
			{typeof(float), "Float"},
			{typeof(double), "Double"},
			{typeof(decimal), "Decimal"},
			{typeof(Vector2), "Vector2"},
			{typeof(Vector3), "Vector3"},
			{typeof(Vector4), "Vector4"},
			{typeof(GameObject), "GameObject"},
			{typeof(NetworkConnectionToClient), "NetworkConnectionToClient"}
		};

		/// <summary>
		/// Split arguments of an entered command
		/// </summary>
		private static List<string> commandArguments = new List<string>(8);

		/// <summary>
		/// Command parameter delimeter groups
		/// </summary>
		private static readonly string[] inputDelimiters = new string[] {"\"\"", "{}", "()", "[]"};

		public static bool isInitialized = false;

		static DebugLogConsole()
		{
			InitializeAsync();
		}

		public static void InitializeAsync()
		{
			if (isInitialized) return;
			// FUCK YOU
			// DO NOT REMOVE THIS UNLESS YOU WANT SOMEONE IN THE FUTURE TO LOSE HAIR WHILE TRYING TO DEBUG THIS SHIT
			Loggy.SetLogLevel(Category.DebugConsole, LogLevel.Trace);
			Loggy.SetLogLevel(Category.Admin, LogLevel.Trace);
			Loggy.SetLogLevel(Category.Rcon, LogLevel.Trace);
			var data = AllowedReflection.GetFunctionsWithAttribute<ConsoleMethodAttribute>();
			Debug.Log($"Attempting to register {data.Count} console commands");

			foreach (var MonoBehaviourAndMethodInfo in data)
			{
				foreach (var type in MonoBehaviourAndMethodInfo.Value)
				{
					try
					{
						AddCommand(type.Attribute.Command, type.Attribute.Description, type.MethodInfo);
					}
					catch (Exception e)
					{
						Loggy.Error(e.ToString());
					}
				}
			}

			isInitialized = true;
		}

		/// <summary>
		/// Used for appending string-based system info to the stringBuilder
		/// </summary>
		/// <returns>StringBuilder object with appended string system info</returns>
		public static StringBuilder AppendSysInfoIfPresent(this StringBuilder sb, string info, string postfix = null)
		{
			if (info != SystemInfo.unsupportedIdentifier)
			{
				sb.Append(info);

				if (postfix != null)
					sb.Append(postfix);

				sb.Append(" ");
			}

			return sb;
		}

		/// <summary>
		/// Used for appending integer-based system info to the stringBuilder
		/// </summary>
		/// <returns>StringBuilder object with appended integer system info</returns>
		public static StringBuilder AppendSysInfoIfPresent(this StringBuilder sb, int info, string postfix = null)
		{
			if (info > 0)
			{
				sb.Append(info);

				if (postfix != null)
					sb.Append(postfix);

				sb.Append(" ");
			}

			return sb;
		}

		/// <summary>
		/// Remove a command from the console
		/// </summary>
		/// <param name="command">Name of command to remove</param>
		public static void RemoveCommand(string command)
		{
			if (string.IsNullOrEmpty(command) == false)
				RegisteredMethodInfos.Remove(command);
		}

		/// <summary>
		/// Create the ConsoleMethodInfo object
		/// </summary>
		/// <param name="command">Name of command to create</param>
		/// <param name="description">Description of the command</param>
		/// <param name="method">MethodInfo object derived from method's name</param>
		/// <param name="instance">Object instance for instance functions</param>
		private static void AddCommand(string command, string description, MethodInfo method, object instance = null)
		{
			Loggy.Info($"Adding command: {command}\n instance: {instance}\n methodInfo: {method.Name}", Category.DebugConsole);
			// Fetch the parameters of the class
			ParameterInfo[] parameters = method.GetParameters();

			bool isMethodValid = true;

			// Store the parameter types in an array
			Type[] parameterTypes = new Type[parameters.Length];
			for (int k = 0; k < parameters.Length; k++)
			{
				Type parameterType = parameters[k].ParameterType;
				if (parseFunctions.ContainsKey(parameterType))
				{
					parameterTypes[k] = parameterType;
				}
				else
				{
					Loggy.Error(
						$"command: {command} has unsupported parameter type: {parameterType.Name}",
						Category.DebugConsole);
					isMethodValid = false;
					break;
				}
			}

			// If method is valid, associate it with the entered command
			if (isMethodValid)
			{
				StringBuilder methodSignature = new StringBuilder(256);
				methodSignature.Append(command).Append(": ");

				if (string.IsNullOrEmpty(description) == false)
					methodSignature.Append(description).Append(" -> ");

				methodSignature.Append(method.DeclaringType.ToString()).Append(".").Append(method.Name).Append("(");
				for (int i = 0; i < parameterTypes.Length; i++)
				{
					Type type = parameterTypes[i];
					string typeName;
					if (typeReadableNames.TryGetValue(type, out typeName) == false)
						typeName = type.Name;

					methodSignature.Append(typeName);

					if (i < parameterTypes.Length - 1)
						methodSignature.Append(", ");
				}

				methodSignature.Append(")");

				Type returnType = method.ReturnType;
				if (returnType != typeof(void))
				{
					string returnTypeName;
					if (typeReadableNames.TryGetValue(returnType, out returnTypeName) == false)
						returnTypeName = returnType.Name;

					methodSignature.Append(" : ").Append(returnTypeName);
				}

				RegisteredMethodInfos[command] = new ConsoleMethodInfo(method, parameterTypes, instance, methodSignature.ToString());
			}
			else
			{
				Loggy.Error($"command: {command} is not valid.", Category.DebugConsole);
			}
		}

		/// <summary>
		/// Parse the command and try to execute it
		/// </summary>
		/// <param name="command">Name of command to execute</param>
		public static void ExecuteCommand(string command)
		{
			if (command == null)
			{
				Loggy.Warning($"{command} does not exist.");
				return;
			}

			command = command.Trim();

			if (command.Length == 0)
			{
				Loggy.Warning($"{command} does not exist.");
				return;
			}

			// Parse the arguments
			commandArguments.Clear();

			int endIndex = IndexOfChar(command, ' ', 0);
			commandArguments.Add(command.Substring(0, endIndex));

			for (int i = endIndex + 1; i < command.Length; i++)
			{
				if (command[i] == ' ')
					continue;

				int delimiterIndex = IndexOfDelimiter(command[i]);
				if (delimiterIndex >= 0)
				{
					endIndex = IndexOfChar(command, inputDelimiters[delimiterIndex][1], i + 1);
					commandArguments.Add(command.Substring(i + 1, endIndex - i - 1));
				}
				else
				{
					endIndex = IndexOfChar(command, ' ', i + 1);
					commandArguments.Add(command.Substring(i, endIndex - i));
				}

				i = endIndex;
			}

			// Check if command exists
			ConsoleMethodInfo methodInfo;
			if (RegisteredMethodInfos.TryGetValue(commandArguments[0], out methodInfo) == false)
				Loggy.Warning("Can't find command: " + commandArguments[0], Category.DebugConsole);
			else if (methodInfo.IsValid() == false)
				Loggy.Warning("Method no longer valid (instance dead): " + commandArguments[0],
					Category.DebugConsole);
			else
			{
				// Check if number of parameter match
				if (methodInfo.parameterTypes.Length != commandArguments.Count - 1)
				{
					Loggy.Warning(
						"Parameter count mismatch: " + methodInfo.parameterTypes.Length + " parameters are needed",
						Category.DebugConsole);
					return;
				}

				Loggy.Trace("Executing command: " + commandArguments[0], Category.DebugConsole);

				// Parse the parameters into objects
				object[] parameters = new object[methodInfo.parameterTypes.Length];
				for (int i = 0; i < methodInfo.parameterTypes.Length; i++)
				{
					string argument = commandArguments[i + 1];

					Type parameterType = methodInfo.parameterTypes[i];
					ParseFunction parseFunction;
					if (parseFunctions.TryGetValue(parameterType, out parseFunction) == false)
					{
						Loggy.Error("Unsupported parameter type: " + parameterType.Name, Category.DebugConsole);
						return;
					}

					object val;
					if (parseFunction(argument, out val) == false)
					{
						Loggy.Error("Couldn't parse " + argument + " to " + parameterType.Name,
							Category.DebugConsole);
						return;
					}

					parameters[i] = val;
				}

				// Execute the method associated with the command
				object result = AllowedReflection.InvokeFunction(methodInfo.method, methodInfo.instance, parameters);
				if (methodInfo.method.ReturnType != typeof(void))
				{
					// Print the returned value to the console
					if (result == null || result.Equals(null))
						Loggy.Info("Value returned: null", Category.DebugConsole);
					else
						Loggy.Info("Value returned: " + result.ToString(), Category.DebugConsole);
				}
			}
		}

		/// <summary>
		/// Find the index of the delimiter group that 'c' belongs to
		/// </summary>
		/// <param name="c">The value of the current iteration of ExecuteCommand.command[i]</param>
		/// <returns>The value of the delimiter after checking</returns>
		private static int IndexOfDelimiter(char c)
		{
			for (int i = 0; i < inputDelimiters.Length; i++)
			{
				if (c == inputDelimiters[i][0])
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Find the index of char in the string, or return the length of string instead of -1
		/// </summary>
		/// <param name="command">Name of command</param>
		/// <param name="c">Current index</param>
		/// <param name="startIndex">Index to start String.indexOf() search</param>
		/// <returns></returns>
		private static int IndexOfChar(string command, char c, int startIndex)
		{
			int result = command.IndexOf(c, startIndex);
			if (result < 0)
				result = command.Length;

			return result;
		}

		private static bool ParseString(string input, out object output)
		{
			output = input;
			return input.Length > 0;
		}

		private static bool ParseBool(string input, out object output)
		{
			if (input == "1" || input.ToLowerInvariant() == "true")
			{
				output = true;
				return true;
			}

			if (input == "0" || input.ToLowerInvariant() == "false")
			{
				output = false;
				return true;
			}

			output = false;
			return false;
		}

		private static bool ParseInt(string input, out object output)
		{
			bool result;
			int value;
			result = int.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseUInt(string input, out object output)
		{
			bool result;
			uint value;
			result = uint.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseLong(string input, out object output)
		{
			bool result;
			long value;
			result = long.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseULong(string input, out object output)
		{
			bool result;
			ulong value;
			result = ulong.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseByte(string input, out object output)
		{
			bool result;
			byte value;
			result = byte.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseSByte(string input, out object output)
		{
			bool result;
			sbyte value;
			result = sbyte.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseShort(string input, out object output)
		{
			bool result;
			short value;
			result = short.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseUShort(string input, out object output)
		{
			bool result;
			ushort value;
			result = ushort.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseChar(string input, out object output)
		{
			bool result;
			char value;
			result = char.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseFloat(string input, out object output)
		{
			bool result;
			float value;
			result = float.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseDouble(string input, out object output)
		{
			bool result;
			double value;
			result = double.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseDecimal(string input, out object output)
		{
			bool result;
			decimal value;
			result = decimal.TryParse(input, out value);

			output = value;
			return result;
		}

		private static bool ParseVector2(string input, out object output)
		{
			return CreateVectorFromInput(input, typeof(Vector2), out output);
		}

		private static bool ParseVector3(string input, out object output)
		{
			return CreateVectorFromInput(input, typeof(Vector3), out output);
		}

		private static bool ParseVector4(string input, out object output)
		{
			return CreateVectorFromInput(input, typeof(Vector4), out output);
		}

		private static bool ParseGameObject(string input, out object output)
		{
			output = GameObject.Find(input);
			return true;
		}

		private static bool ParseNetworkConnectionToClient(string input, out object output)
		{
			output = null;
			foreach (var player in PlayerList.Instance.AllPlayers)
			{
				if (player.ClientId == input || player.Username == input)
				{
					output = player.Connection;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Create a vector of specified type (fill the blank slots with 0 or ignore unnecessary slots)
		/// </summary>
		private static bool CreateVectorFromInput(string input, Type vectorType, out object output)
		{
			List<string> tokens = new List<string>(input.Replace(',', ' ').Trim().Split(' '));

			int i;
			for (i = tokens.Count - 1; i >= 0; i--)
			{
				tokens[i] = tokens[i].Trim();
				if (tokens[i].Length == 0)
					tokens.RemoveAt(i);
			}

			float[] tokenValues = new float[tokens.Count];
			for (i = 0; i < tokens.Count; i++)
			{
				float val;
				if (float.TryParse(tokens[i], out val) == false)
				{
					if (vectorType == typeof(Vector3))
						output = new Vector3();
					else if (vectorType == typeof(Vector2))
						output = new Vector2();
					else
						output = new Vector4();

					return false;
				}

				tokenValues[i] = val;
			}

			if (vectorType == typeof(Vector3))
			{
				Vector3 result = new Vector3();

				for (i = 0; i < tokenValues.Length && i < 3; i++)
					result[i] = tokenValues[i];

				for (; i < 3; i++)
					result[i] = 0;

				output = result;
			}
			else if (vectorType == typeof(Vector2))
			{
				Vector2 result = new Vector2();

				for (i = 0; i < tokenValues.Length && i < 2; i++)
					result[i] = tokenValues[i];

				for (; i < 2; i++)
					result[i] = 0;

				output = result;
			}
			else
			{
				Vector4 result = new Vector4();

				for (i = 0; i < tokenValues.Length && i < 4; i++)
					result[i] = tokenValues[i];

				for (; i < 4; i++)
					result[i] = 0;

				output = result;
			}

			return true;
		}
	}
}