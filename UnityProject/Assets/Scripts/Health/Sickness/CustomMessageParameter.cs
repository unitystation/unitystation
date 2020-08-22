using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Health.Sickness
{
	public class CustomMessage : MonoBehaviour
	{
		public string privateMessage;
		public string publicMessage;
	}

	public class CustomMessageParameter: BaseSymptomParameter
	{
		[Tooltip("A list of random custom message to show the player and/or the observers")]
		public List<CustomMessage> customMessages;

		public CustomMessageParameter()
		{
			customMessages = new List<CustomMessage>();

			// Add at least one element.
			customMessages.Add(new CustomMessage());
		}
	}
}
