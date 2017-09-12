using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using Sprites;
using UnityEditor;
using System.Linq;

public class DoorAnimator : MonoBehaviour
{
	private DoorController doorController;
    private SpriteRenderer overlay_Lights;
    private SpriteRenderer overlay_Glass;
    private SpriteRenderer doorbase;
    public Sprite[] doorBaseSprites;
    public Sprite[] overlaySprites;
    public Sprite[] overlayLights;


    void Start()
    {
        doorController = GetComponent<DoorController>();
        foreach (Transform child in transform)
        {
            //loading the spritesLists without the use of the list of sprites from spritetype.
            switch (child.gameObject.name)
            {   
                
                case "doorbase":
                    doorbase = child.gameObject.GetComponent<SpriteRenderer>();
                    doorBaseSprites = GetListOfSpritesFromLoadedSprite(doorbase.sprite).ToArray();

                    break;
                case "overlay_Glass":
                    overlay_Glass = child.gameObject.GetComponent<SpriteRenderer>();                    
                    overlaySprites = GetListOfSpritesFromLoadedSprite(overlay_Glass.sprite).ToArray();
                    break;
                case "overlay_Lights":
                    overlay_Lights = child.gameObject.GetComponent<SpriteRenderer>();
                    overlayLights = GetListOfSpritesFromLoadedSprite(overlay_Lights.sprite).ToArray();
                    break;                
            }
        }
        
    }
    //getting the sprites from the prefab using the reference sprite
    public Sprite[] GetListOfSpritesFromLoadedSprite(Sprite sprite) {
        string basepath = AssetDatabase.GetAssetPath(sprite).Replace("Assets/Resources/","");        
        return Resources.LoadAll<Sprite>(basepath.Replace("png",""));
    }
    

    public void AccessDenied()
    {
        doorController.isPerformingAction = true;
        StartCoroutine(_AccessDenied());
    }

    IEnumerator _AccessDenied()
    {
        SoundManager.PlayAtPosition("AccessDenied", transform.position);
        int loops = 0;
        while (loops < 4)
        {
            loops++;
            if (overlay_Lights.sprite == null)
            {
                overlay_Lights.sprite = overlaySprites[15];
            }
            else
            {
                overlay_Lights.sprite = null;
            }
            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(0.2f);
        doorController.isPerformingAction = false;
    }

    public void OpenDoor()
    {
        doorController.isPerformingAction = true;
        StartCoroutine(_OpenDoor());
    }

    IEnumerator _OpenDoor()
    {
        doorbase.sprite = doorBaseSprites[0];
        if (doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = overlaySprites[39];
        }
        else
        {
            overlay_Glass.sprite = doorBaseSprites[15];
        }
        overlay_Lights.sprite = null;
        doorController.PlayOpenSound();
        yield return new WaitForSeconds(0.03f);
        overlay_Lights.sprite = overlaySprites[0];
        yield return new WaitForSeconds(0.06f);
        overlay_Lights.sprite = null;
        yield return new WaitForSeconds(0.09f);
        overlay_Lights.sprite = overlaySprites[0];
        yield return new WaitForSeconds(0.12f);
        doorbase.sprite = doorBaseSprites[3];
        if (doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = overlaySprites[41];
        }
        else
        {
            overlay_Glass.sprite = doorBaseSprites[19];
        }
        overlay_Lights.sprite = overlaySprites[1];
        yield return new WaitForSeconds(0.15f);
        doorbase.sprite = doorBaseSprites[4];
        if (doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = overlaySprites[42];
        }
        else
        {
            overlay_Glass.sprite = doorBaseSprites[20];
        }
        overlay_Lights.sprite = overlaySprites[2];
        doorController.BoxCollToggleOff();
        yield return new WaitForSeconds(0.2f);
        doorbase.sprite = doorBaseSprites[5];
        if (doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = overlaySprites[43];
        }
        else
        {
            overlay_Glass.sprite = doorBaseSprites[21];
        }
        overlay_Lights.sprite = overlaySprites[3];
        if (doorbase.isVisible)
            EventManager.Broadcast(EVENT.UpdateFov);
        yield return new WaitForSeconds(0.2f);
        doorbase.sprite = doorBaseSprites[6];
        overlay_Lights.sprite = overlaySprites[4];
        yield return new WaitForSeconds(0.2f);
        doorbase.sprite = doorBaseSprites[7];
        overlay_Lights.sprite = null;
        yield return new WaitForSeconds(0.2f);
        doorbase.sprite = doorBaseSprites[8];
        yield return new WaitForEndOfFrame();
        doorController.isPerformingAction = false;
    }

    public void CloseDoor()
    {
        doorController.isPerformingAction = true;
        StartCoroutine(_CloseDoor());
    }

    IEnumerator _CloseDoor()
    {
        doorbase.sprite = doorBaseSprites[8];
        overlay_Lights.sprite = overlaySprites[5];
        yield return new WaitForSeconds(0.03f);
        doorbase.sprite = doorBaseSprites[9];
        overlay_Lights.sprite = overlaySprites[4];
        doorController.PlayCloseSFXshort();
        yield return new WaitForSeconds(0.04f);
        doorController.BoxCollToggleOn();
        yield return new WaitForSeconds(0.06f);
        doorbase.sprite = doorBaseSprites[10];
        if (doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = overlaySprites[43];
        }
        else
        {
            overlay_Glass.sprite = doorBaseSprites[21];
        }
        overlay_Lights.sprite = overlaySprites[3];
        yield return new WaitForSeconds(0.09f);
        doorbase.sprite = doorBaseSprites[11];
        if (doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = overlaySprites[42];
        }
        else
        {
            overlay_Glass.sprite = doorBaseSprites[20];
        }
        overlay_Lights.sprite = overlaySprites[2];
        yield return new WaitForSeconds(0.12f);
        doorbase.sprite = doorBaseSprites[12];
        if (!doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = doorBaseSprites[19];
        }
        overlay_Lights.sprite = overlaySprites[1];
        yield return new WaitForSeconds(0.15f);
        doorbase.sprite = doorBaseSprites[13];
        if (doorController.isWindowedDoor)
        {
            overlay_Glass.sprite = overlaySprites[39];
        }
        else
        {
            overlay_Glass.sprite = doorBaseSprites[15];
        }
        overlay_Lights.sprite = overlaySprites[0];
        if (doorbase.isVisible)
            EventManager.Broadcast(EVENT.UpdateFov);
        yield return new WaitForSeconds(0.18f);
        overlay_Lights.sprite = null;
        yield return new WaitForSeconds(0.20f);
        doorbase.sprite = doorBaseSprites[13];
        yield return new WaitForEndOfFrame();
        doorController.isPerformingAction = false;
    }
}
