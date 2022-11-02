using System.Collections.Generic;
using Mirror;
using ScriptableObjects.Characters;
using UnityEngine;

namespace Core.Characters
{
	public class CharacterAttributes : NetworkBehaviour
	{
		private readonly List<CharacterAttribute> attributes = new List<CharacterAttribute>();

		[Server]
		public void AddAttributes(List<CharacterAttribute> newAttributes)
		{
			foreach (var attribute in newAttributes)
			{
				SetupNewAttribute(attribute);
			}
			Debug.Log(attributes.Count);
		}

		public bool HasAttribute(CharacterAttribute givenAttribute)
		{
			return attributes.Contains(givenAttribute);
		}

		[Server]
		private void SetupNewAttribute(CharacterAttribute attribute)
		{
			if (attribute.CanHaveTwoOfThis == false && attributes.Contains(attribute)) return;
			attributes.Add(attribute);
			if(attribute.OnAddBehaviors.Count == 0) return;
			foreach (var behavior in attribute.OnAddBehaviors)
			{
				if (behavior.TryGetComponent<CharacterAttributeBehavior>(out var staticBehavior) && staticBehavior.Spawn)
				{
					// If we don't want to spawn this object and run it's code directly
					staticBehavior.Run(gameObject);
					continue;
				}
				// if we want to spawn this object while having it's component persistent on a gameObject in the game world
				var behaviorObject = Spawn.ServerPrefab(behavior);
				if(behaviorObject.Successful == false) continue;
				if(behaviorObject.GameObject.TryGetComponent<CharacterAttributeBehavior>(out var behaviorScript) == false) continue;
				behaviorScript.Run(gameObject);
			}
		}
	}
}