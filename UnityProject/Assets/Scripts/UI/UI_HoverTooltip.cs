using UnityEngine;
using UnityEngine.EventSystems;


public class UI_HoverTooltip : TooltipMonoBehaviour
{
	public bool useObjectName;

	[ConditionalField("useObjectName", false)]
	public string hoverName;

	public override string Tooltip => useObjectName ? name : hoverName;
}