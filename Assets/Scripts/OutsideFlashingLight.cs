using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutsideFlashingLight : MonoBehaviour
{

    public SpriteRenderer lightSprite;
    public Color spriteOnCol;
    public Color spriteOffCol;

    public GameObject lightSource;

    public float flashWaitTime = 1f;
    private float timeCount = 0f;

    void Update()
    {
        timeCount += Time.deltaTime;
        if (timeCount >= flashWaitTime)
        {
            timeCount = 0f;
            SwitchLights();
        }
    }

    void SwitchLights()
    {
        if (!lightSource.activeSelf)
        {
            lightSource.SetActive(true);
            lightSprite.color = spriteOnCol;
        }
        else
        {
            lightSource.SetActive(false);
            lightSprite.color = spriteOffCol;
        }

    }
}
