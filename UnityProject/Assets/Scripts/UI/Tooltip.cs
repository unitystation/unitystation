using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static GameObject tooltipResource = null;
    const float TOOLTIP_INTERVAL = 1.0f;
    private float enterTime = 0;
    private GameObject tooltipObject = null;
    [SerializeField]
    private string tooltipText = "";

    void Start()
    {
        if (tooltipResource == null) {
            tooltipResource = Resources.Load<GameObject>("UI/GUI/Tooltip");
        }
    }

    void Update()
    {
        if (tooltipObject) {
            // Move tooltip to mouse
            tooltipObject.transform.position = Input.mousePosition - new Vector3(0, 20, 0);
        } else if (enterTime != 0 && Time.realtimeSinceStartup - enterTime > TOOLTIP_INTERVAL) { 
            // Create tooltip if needed
            tooltipObject = Instantiate(tooltipResource, Vector3.zero, Quaternion.identity);
            // While the tooltip exists, we place it under the canvas so it'll be in the top layer
            tooltipObject.transform.SetParent(this.GetComponentInParent<Canvas>().transform);
            tooltipObject.transform.SetAsLastSibling();
            tooltipObject.GetComponentInChildren<Text>().text = tooltipText;
        }
    }

    public void SetText(string newText) {
        tooltipText = newText;
        if (tooltipObject) {
            tooltipObject.GetComponentInChildren<Text>().text = tooltipText;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        enterTime = Time.realtimeSinceStartup;
    }

    public void OnPointerExit(PointerEventData eventData) {
        enterTime = 0;
        
        if (tooltipObject) {
            Destroy(tooltipObject);
        }
    }
}
