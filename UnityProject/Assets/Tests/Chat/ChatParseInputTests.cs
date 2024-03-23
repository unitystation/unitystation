using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
	public class ChatParseInputTests
	{
		private class MockedChatContext : IChatInputContext
		{
			public ChatChannel DefaultChannel => ChatChannel.Command;
		}
		private MockedChatContext context = new MockedChatContext();

		// Some test cases that can brack something
		private static readonly object[] InputTestCases =
		{
			// Invalid syntax
			new object[] {null, ChatChannel.None, null },
			new object[] {"", ChatChannel.None, "" },
			new object[] {":", ChatChannel.None, ":" },
			new object[] {".", ChatChannel.None, "." },
			new object[] { "Hello World", ChatChannel.None, "Hello World" },

			// Common Channel
			new object[] {";", ChatChannel.Common, "" },
			new object[] {";Hello World",  ChatChannel.Common, "Hello World"},
			new object[] {"; Hello World",  ChatChannel.Common, "Hello World"},
			new object[] { "Hello World;",  ChatChannel.None, "Hello World;"},

			// Department Specific
			new object[] {":s", ChatChannel.Security, "" },
			new object[] {":sFind the clown!",  ChatChannel.Security, "Find the clown!"},
			new object[] {":s Find the clown!",  ChatChannel.Security, "Find the clown!"},
			new object[] {":sc Find the clown!",  ChatChannel.Security, "c Find the clown!"},
			new object[] {"List: one and two",  ChatChannel.None, "List: one and two"},

			// Default Channel - mock context always return command
			new object[] {":h Heads, report!", ChatChannel.Command, "Heads, report!" },
		};

		[TestCaseSource(nameof(InputTestCases))]
        public void CheckChannelAndClearMessage(string rawMsg, ChatChannel expectedChannel, string clearMsg)
        {
			var parsedInfo = Chat.ParsePlayerInput(rawMsg, context);
			Assert.AreEqual(parsedInfo.ParsedChannel, expectedChannel);
			Assert.AreEqual(parsedInfo.ClearMessage, clearMsg);
		}

		[Test]
		public void TestAllChannelsTag()
		{
			foreach (var pair in Chat.ChannelsTags)
			{
				var msg = string.Format(":{0} Testing!", pair.Key);
				CheckChannelAndClearMessage(msg, pair.Value, "Testing!");

				msg = string.Format(".{0} Testing!", pair.Key);
				CheckChannelAndClearMessage(msg, pair.Value, "Testing!");
			}
		}

	}
}
