using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shake : MonoBehaviour
{
    /// <summary>
    /// This effect shakes a gameObjects for a set amount of time with controls for
    /// how intense the animation should be.
    /// This effect is mainly intended for stationary objects but it might work with moving ones.
    /// </summary>

    private Vector3 originalPosition;

    [SerializeField, Tooltip("Which Axis will the animation play on?.")]
    private AxisMode axisMode = AxisMode.X;

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
        transform.position = originalPosition;
    }
 
    private IEnumerator Shaking(float duration, float distance, float delayBetweenShakes)
    {
        float timer = 0f;
 
        while (timer < duration)
        {
            timer += Time.deltaTime;
 
            Vector3 randomPosition = originalPosition + (Random.insideUnitSphere * distance);
 
            animatePosition(randomPosition);

            if (delayBetweenShakes > 0f)
            {
                yield return new WaitForSeconds(delayBetweenShakes);
            }
            else
            {
                yield return null;
            }
        }

        LeanTween.move(gameObject, originalPosition, 0.1f);
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
