using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerExaminationWindowSlot : MonoBehaviour
{
    /// <summary>
    /// Object that will be enabled if slot is obscured
    /// </summary>
    public GameObject obstructedOverlay;
    
    [SerializeField] private UI_ItemSlot itemSlot;
    public UI_ItemSlot UI_ItemSlot => itemSlot;

    public void Reset()
    {
        itemSlot.LinkSlot(null);
        itemSlot.Reset();
        obstructedOverlay.SetActive(false);
    }

    public void SetObscuredOverlayActive(bool active)
    {
        obstructedOverlay.SetActive(active);
    }

    public void OnClick()
    {
        Debug.Log("ON CLICK " + itemSlot.NamedSlot);
    }
}
