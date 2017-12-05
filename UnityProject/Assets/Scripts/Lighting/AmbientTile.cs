using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;

[ExecuteInEditMode]
public class AmbientTile : ObjectTrigger
{

    public Color onColor = new Color32(105, 105, 105, 255);
    public Color offColor = new Color32(0, 0, 0, 255);
    private SpriteRenderer spriteRend;

    void Start()
    {
        spriteRend = GetComponent<SpriteRenderer>();
        spriteRend.color = onColor;
    }

    //LightSource sends a message to this method
    public override void Trigger(bool state)
    {
        if (spriteRend == null)
        {
            spriteRend = GetComponent<SpriteRenderer>();
        }

        spriteRend.color = state ? onColor : offColor;
    }
}

