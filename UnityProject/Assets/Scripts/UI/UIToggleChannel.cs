using UnityEngine;
using UnityEngine.UI;

public class UIToggleChannel : MonoBehaviour
{
	public ChatChannel channel;
	[SerializeField]
	private GameObject tooltip;
	private Text tooltipText;
	private RectTransform tooltipRect;
	private void Start()
	{
		tooltipRect = tooltip.GetComponent<RectTransform>();
		tooltipText = tooltip.GetComponentInChildren<Text>();
		tooltipText.text = channel.ToString();
	}

	public void ToggleTooltip(bool isOn)
	{
		if (isOn)
		{
			// leftXCoord is calculated using the centre x coord of the tooltip box, minus half of its width
			//     [ ]
			// [<---X    ]
			float leftXCoord = gameObject.transform.localPosition.x-tooltipRect.rect.width/2;
			// Logger.Log(gameObject.name + " localPos: " + gameObject.transform.localPosition.x);
			// Logger.Log("Half width: " + tooltipRect.rect.width/2);
			// Logger.Log(gameObject.name + " LeftXCoord: " + leftXCoord);

			// Since we are using local pos, if it's less than 0, it's off screen
			if (leftXCoord < 0)
			{
				// Logger.Log("Less than 0! Moving");
				RectTransform thisRect = gameObject.GetComponent<RectTransform>();
				// Move the tooltip box so it is left aligned with the ChannelToggle box
				// (which shouldn't be off screen)
				//    [ ]
				// -->[      ]
				tooltip.transform.localPosition = new Vector3(tooltipRect.rect.width/2 - thisRect.rect.width/2, tooltip.transform.localPosition.y);

				// All this just to move a fucking box
			}
		}
		tooltip.SetActive(isOn);
	}
}