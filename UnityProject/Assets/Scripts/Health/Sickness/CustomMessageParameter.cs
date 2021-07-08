using System.Collections.Generic;
using UnityEngine;

namespace Health.Sickness
{
	public class CustomMessage
	{
		public string privateMessage;
		public string publicMessage;
	}

	public class CustomMessageParameter: BaseSymptomParameter
	{
		[Tooltip("A list of random custom message to show the player and/or the observers")]
		public List<CustomMessage> CustomMessages;

		public CustomMessageParameter()
		{
			CustomMessages = new List<CustomMessage>();
		}
	}
}
