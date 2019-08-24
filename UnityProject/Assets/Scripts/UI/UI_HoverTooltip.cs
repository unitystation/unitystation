using UnityEngine;
using UnityEngine.EventSystems;


public class UI_HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public bool useObjectName;

	[ConditionalField("useObjectName", false)]
    public string hoverName;

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (useObjectName)
		{
        	UIManager.SetToolTip = name;
		}
		else
		{
	        UIManager.SetToolTip = hoverName;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
        UIManager.SetToolTip = "";
	}
}