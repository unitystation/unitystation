using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_HackingWire : MonoBehaviour
{
	[SerializeField]
	private GameObject wireStart;
	private GUI_HackingNode startNode;

	[SerializeField]
	private GameObject wireEnd;
	private GUI_HackingNode endNode;

	[SerializeField]
	private GameObject wireBody;

	private Vector2 uiOffset;
	private Canvas canvas;

	public void Start()
	{
	}

	public void SetStartUINode(GUI_HackingNode startNode)
	{
		this.startNode = startNode;
		RectTransform nodeRectTransform = startNode.GetComponent<RectTransform>();
		RectTransform wireStartRectTransform = wireStart.GetComponent<RectTransform>();

		wireStartRectTransform.position = nodeRectTransform.position;// + nodeRectTransform.anchoredPosition;
	}

	public void SetEndUINode(GUI_HackingNode endNode)
	{
		this.endNode = endNode;
		RectTransform nodeRectTransform = endNode.GetComponent<RectTransform>();
		RectTransform wireEndRectTransform = wireEnd.GetComponent<RectTransform>();

		wireEndRectTransform.position = nodeRectTransform.position;

	}

	public void PositionWireBody()
	{
		RectTransform wireStartRectTransform = wireStart.GetComponent<RectTransform>();
		RectTransform wireEndRectTransform = wireEnd.GetComponent<RectTransform>();
		RectTransform wireBodyRectTransform = wireBody.GetComponent<RectTransform>();

		Vector2 dif = (wireEndRectTransform.position - wireStartRectTransform.position);

		Vector2 norm = dif.normalized;
		float dist = dif.magnitude;
		float angle = -Vector2.SignedAngle(norm, Vector2.up);

		Vector2 wireOrigin = dist * 0.5f * norm + (Vector2)wireStartRectTransform.position;

		wireBodyRectTransform.position = wireOrigin;

		Vector2 oldSize = wireBodyRectTransform.sizeDelta;

		wireBodyRectTransform.sizeDelta = new Vector2(oldSize.x, dist * (2 - UIManager.Instance.transform.localScale.x)); //Need to add this scaling here, because for some reason, the entire UI is scaled by 0.67? Iunno why.
		
		Vector3 rotation = wireBodyRectTransform.transform.eulerAngles;
		rotation.z = angle;
		wireBodyRectTransform.transform.eulerAngles = rotation;
	}
}
