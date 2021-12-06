using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private GameObject tooltipTemplate = null;
    const float TOOLTIP_INTERVAL = 1.0f;
    private float enterTime = 0;
    private GameObject tooltipObject = null;
    [SerializeField]
    private string tooltipText = "";

    void Start()
    {
        tooltipObject = Instantiate(tooltipTemplate, Vector3.zero, Quaternion.identity);
        tooltipObject.GetComponentInChildren<Text>().text = tooltipText;
        // While the tooltip exists, we place it under the canvas so it'll be in the top layer
        tooltipObject.transform.SetParent(this.GetComponentInParent<Canvas>().transform);
        tooltipObject.SetActive(false);
    }

    private void OnEnable()
    {
	    UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
    }

    private void OnDisable()
    {
	    UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
    }

    void UpdateMe()
    {
        if (tooltipObject.activeSelf) {
            // Move tooltip to mouse
            tooltipObject.transform.position = CommonInput.mousePosition - new Vector3(0, 20, 0);
        } else if (enterTime != 0 && Time.realtimeSinceStartup - enterTime > TOOLTIP_INTERVAL) {
            // Move tooltip above all other layers. We do it now so new objects wont hide it.
            tooltipObject.transform.SetAsLastSibling();
            tooltipObject.SetActive(true);
            tooltipObject.transform.position = CommonInput.mousePosition - new Vector3(0, 20, 0);
        }
    }

    public void SetText(string newText) {
        tooltipText = newText;
        tooltipObject.GetComponentsInChildren<Text>(false)[0].text = tooltipText;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        enterTime = Time.realtimeSinceStartup;
    }

    public void OnPointerExit(PointerEventData eventData) {
        enterTime = 0;

        tooltipObject.SetActive(false);
    }
}
