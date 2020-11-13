using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerExaminationWindowSlot : MonoBehaviour
{
    /// <summary>
    /// Object that will be enabled if slot is obscured
    /// </summary>
    [SerializeField]
    private GameObject obstructedOverlay;
    /// <summary>
    /// Object that will be enabled when player interacts and slot is obscured
    /// </summary>
    [SerializeField]
    private GameObject questionMark;

    [System.NonSerialized]
    public PlayerExaminationWindowUI parent;
    
    [SerializeField] private UI_ItemSlot itemSlot;
    public UI_ItemSlot UI_ItemSlot => itemSlot;

    public bool IsObscured => obstructedOverlay.activeSelf;
    public bool IsPocket => itemSlot.NamedSlot == NamedSlot.storage01 
                            || itemSlot.NamedSlot == NamedSlot.storage02
                            || itemSlot.NamedSlot == NamedSlot.storage03;
    
    public bool IsQuestionMarkActive => questionMark.activeSelf;

    public void Reset()
    {
        itemSlot.LinkSlot(null);
        itemSlot.Reset();
        obstructedOverlay.SetActive(false);
        questionMark.SetActive(false);
    }

    public void SetObscuredOverlayActive(bool active)
    {
        obstructedOverlay.SetActive(active);
    }

    public void SetQuestionMarkActive(bool active)
    {
        questionMark.SetActive(active);
    }

    public void OnClick()
    {
        parent.TryInteract(this);
    }
}