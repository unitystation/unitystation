using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;
using US13.Items.Weapons.Melee;

public class ItemMeleeBehaviours : MonoBehaviour
{
	[SerializeReference, SelectImplementation(typeof(ICustomMeleeBehaviour))]
	public List<ICustomMeleeBehaviour> CustomMeleeBehaviours = new List<ICustomMeleeBehaviour>();
}
