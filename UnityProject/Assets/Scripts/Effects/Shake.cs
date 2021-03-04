using System.Collections;
using UnityEngine;
using Mirror;

public class Shake : LTEffect
{
    /// <summary>
    /// A shake effect that shakes an entire GameObject or it's sprites only.
    /// </summary>

    private float shakeDuration;
    private float shakeDistance;
    private float delayBetweenShakes;
    private void Awake()
    {
       getOriginalPosition();
    }

    private void storeShakeData(float duration, float distance, float delay)
    {
        shakeDistance = distance;
        shakeDuration = duration;
        delayBetweenShakes = delay;
    }

    public void startShake(float duration, float distance, float delay)
    {
        storeShakeData(duration, distance, delay);
        StartCoroutine(Shaking(shakeDuration, shakeDistance, delayBetweenShakes)); // <-- So the server can see the animation.
        CmdStartAnimation();
    }

    [Server]
    public override void CmdStartAnimation()
    {
        StartCoroutine(Shaking(shakeDuration, shakeDistance, delayBetweenShakes));
    }

    [Server]
    public override void CmdStopAnimation()
    {
        haltShake();
        base.CmdStopAnimation();
    }
    public void haltShake()
    {
        StopAllCoroutines();
        if(animType == AnimMode.GAMEOBJECT)
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
            
            switch (animType)
            {
                case AnimMode.GAMEOBJECT:
                    animatePosition(randomPosition);
                    break;
                case AnimMode.SPRITE:
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

        switch (animType)
        {
            case AnimMode.GAMEOBJECT:
                LeanTween.move(gameObject, originalPosition, 0.1f);
                tween.isAnim = false;
                break;
            case AnimMode.SPRITE:
                LeanTween.move(spriteReference.gameObject, originalPosition, 0.1f);
                tween.isAnim = false;
                break;
        }
        
    }

    private void animateSpritePosition(Vector3 pos)
    {
        tween.RpcMove(axisMode, pos, 0.1f);
    }

    private void animatePosition(Vector3 pos)
    {
        tween.RpcMove(axisMode, pos, 0.1f);
    }
}
