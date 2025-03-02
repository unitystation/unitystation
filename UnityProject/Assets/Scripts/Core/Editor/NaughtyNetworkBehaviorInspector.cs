using Core.Networking;
using NaughtyAttributes.Editor;
using UnityEditor;

namespace Core.Editor
{
	[CustomEditor(typeof(NaughtyNetworkBehaviour), true)]
	public class NaughtyNetworkBehaviourEditor : NaughtyInspector
	{
	}
}