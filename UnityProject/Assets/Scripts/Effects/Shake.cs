using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shake : MonoBehaviour
{
    /// <summary>
    /// A shake effect that comes in two modes, SPRITE and GAMEOBJECT
    /// SPRITE animates a given sprite only.
    /// GAMEOBJECT animates the entire object's position.
    /// </summary>

    private Vector3 originalPosition;

    [SerializeField, Tooltip("Which Axis will the animation play on?")]
    private AxisMode axisMode = AxisMode.X;

    [SerializeField, Tooltip("Do you want to animate the entire gameObject or just the sprite?")]
    private ShakeMode shakeType = ShakeMode.SPRITE;

    [Tooltip("The sprite gameObject that will be used for the shake animation.")]
    public Transform spriteReference;

    private enum ShakeMode
    {
        SPRITE,
        GAMEOBJECT
    }

    private enum AxisMode 
    {
        X,
        XY,
        Y
    }

    private void Awake()
    {
       getOriginalPosition();
    }

    public void startShake(float duration, float distance, float delayBetweenShakes)
    {
        StartCoroutine(Shaking(duration, distance, delayBetweenShakes));
    }

    public void haltShake()
    {
        StopAllCoroutines();
        if(shakeType == ShakeMode.GAMEOBJECT)
        {
            transform.position = originalPosition;
            
        }
        else
        {
            spriteReference.transform.position = new Vector2(0,0);
        }
    }
 
    private IEnumerator Shaking(float duration, float distance, float delayBetweenShakes)
    {
        float timer = 0f;
 
        while (timer < duration)
        {
            timer += Time.deltaTime;
 
            Vector3 randomPosition = originalPosition + (Random.insideUnitSphere * distance);
            
            switch (shakeType)
            {
                case ShakeMode.GAMEOBJECT:
                    animatePosition(randomPosition);
                    break;
                case ShakeMode.SPRITE:
                    animateSpritePosition(randomPosition);
                    break;
            }


            if (delayBetweenShakes > 0f)
            {
                yield return new WaitForSeconds(delayBetweenShakes);
            }
            else
            {
                yield return null;
            }
        }

        switch (shakeType)
        {
            case ShakeMode.GAMEOBJECT:
                LeanTween.move(gameObject, originalPosition, 0.1f);
                break;
            case ShakeMode.SPRITE:
                LeanTween.move(spriteReference.gameObject, originalPosition, 0.1f);
                break;
        }
        
    }

    private void animateSpritePosition(Vector2 pos)
    {
        switch (axisMode)
        {
            case AxisMode.X:
                LeanTween.moveX(spriteReference.gameObject, pos.x, 0.1f);
                break;
            case AxisMode.Y:
                LeanTween.moveY(spriteReference.gameObject, pos.y, 0.1f);
                break;
            case AxisMode.XY:
                LeanTween.move(spriteReference.gameObject, pos, 0.1f);
                break;
        }
    }

    private void animatePosition(Vector3 pos)
    {
        switch (axisMode)
        {
            case AxisMode.X:
                LeanTween.moveX(gameObject, pos.x, 0.1f);
                break;
            case AxisMode.Y:
                LeanTween.moveY(gameObject, pos.y, 0.1f);
                break;
            case AxisMode.XY:
                LeanTween.move(gameObject, pos, 0.1f);
                break;
        }
    }

    public void getOriginalPosition()
    {
        originalPosition = transform.position;
    }
}
