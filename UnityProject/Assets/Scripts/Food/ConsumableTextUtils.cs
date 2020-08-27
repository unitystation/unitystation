using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConsumableTextUtils
{
	public static void SendGenericConsumeMessage(PlayerScript feeder, PlayerScript eater,
		HungerState eaterHungerState, string consumableName, string eatVerb)
	{
		if (feeder == eater) //If you're eating it yourself.
		{
			switch (eaterHungerState)
			{
				case HungerState.Full:
					Chat.AddActionMsgToChat(eater.gameObject, $"You cannot force any more of the {consumableName} to go down your throat!",
					$"{eater.playerName} cannot force any more of the {consumableName} to go down {eater.characterSettings.TheirPronoun()} throat!");
					return; //Not eating!
				case HungerState.Normal:
					Chat.AddActionMsgToChat(eater.gameObject, $"You unwillingly {eatVerb} the {consumableName}.", //"a bit of"
						$"{eater.playerName} unwillingly {eatVerb}s the {consumableName}."); //"a bit of"
					break;
				case HungerState.Hungry:
					Chat.AddActionMsgToChat(eater.gameObject, $"You {eatVerb} the {consumableName}.",
						$"{eater.playerName} {eatVerb}s the {consumableName}.");
					break;
				case HungerState.Malnourished:
					Chat.AddActionMsgToChat(eater.gameObject, $"You hungrily {eatVerb} the {consumableName}.",
						$"{eater.playerName} hungrily {eatVerb}s the {consumableName}.");
					break;
				case HungerState.Starving:
					Chat.AddActionMsgToChat(eater.gameObject, $"You hungrily {eatVerb} the {consumableName}, gobbling it down!",
						$"{eater.playerName} hungrily {eatVerb}s the {consumableName}, gobbling it down!");
					break;
			}
		}
		else //If you're feeding it to someone else.
		{
			if (eaterHungerState == HungerState.Full)
			{
				Chat.AddActionMsgToChat(eater.gameObject,
					$"{feeder.playerName} cannot force any more of {consumableName} down your throat!",
					$"{feeder.playerName} cannot force any more of {consumableName} down {eater.playerName}'s throat!");
				return; //Not eating!
			}
			else
			{
				Chat.AddActionMsgToChat(eater.gameObject,
					$"{feeder.playerName} attempts to feed you {consumableName}.",
					$"{feeder.playerName} attempts to feed {eater.playerName} {consumableName}.");
			}
		}
	}

	public static void SendGenericForceFeedMessage(PlayerScript feeder, PlayerScript eater,
		HungerState eaterHungerState, string consumableName, string eatVerb)
	{
		Chat.AddActionMsgToChat(eater.gameObject,
			$"{feeder.playerName} forces you to {eatVerb} {consumableName}!",
			$"{feeder.playerName} forces {eater.playerName} to {eatVerb} {consumableName}!");
	}
}
