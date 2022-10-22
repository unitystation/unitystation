using UnityEngine;

namespace Objects.Wallmounts.PublicTerminals
{
	public class PublicTerminalModule : MonoBehaviour
	{
		[field: SerializeField] public bool IsActive { get; set; } = true;
		[field: SerializeField] public PublicDepartmentTerminal Terminal { get; set; }
	}
}