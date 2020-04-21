using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHackable
{
	/// <summary>
	/// Generate the hack nodes the object will require. Give them an internal name, a public name and define whether they are inputs or outputs.
	/// </summary>
	/// <returns></returns>
	List<HackingNode> GenerateHackNodes();

	/// <summary>
	/// Link the nodes generated with GenerateHackNodes(). This will allow the object to function normally by using the nodes system instead, which can then be hacked.
	/// </summary>
	void LinkHackNodes();

	List<HackingNode> GetHackingNodes();

}
