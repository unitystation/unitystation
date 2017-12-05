using System.Collections;
using UnityEngine;
using InputControl;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine.Networking;
using Matrix = Tilemaps.Scripts.Matrix;

public class ShutterController : ObjectTrigger
{
    private Animator animator;
    private RegisterDoor _registerTile;
    private Matrix _matrix;

    public bool IsClosed { get; private set; }

    private int closedLayer;
    private int openLayer;
    private int closedSortingLayer;
    private int openSortingLayer;

    //For network sync reliability
    private bool waitToCheckState = false;
    private bool tempStateCache;

    void Awake()
    {
        animator = gameObject.GetComponent<Animator>();
        _registerTile = gameObject.GetComponent<RegisterDoor>();
        _matrix = Matrix.GetMatrix(this);

        closedLayer = LayerMask.NameToLayer("Door Closed");
        closedSortingLayer = SortingLayer.NameToID("Doors Open");
        openLayer = LayerMask.NameToLayer("Door Open");
        openSortingLayer = SortingLayer.NameToID("Doors Closed");
        SetLayer(closedLayer, closedSortingLayer);
    }

    public override void Trigger(bool state)
    {
        tempStateCache = state;
        if (waitToCheckState)
            return;

        if (animator == null)
        {
            waitToCheckState = true;
            return;
        }

        SetState(state);
    }

    private void SetState(bool state)
    {
        IsClosed = state;
        _registerTile.IsClosed = state;
        if (state)
        {
            SetLayer(closedLayer, closedSortingLayer);
            if (isServer)
            {
                DamageOnClose();
            }
        }
        else
        {
            SetLayer(openLayer, openSortingLayer);
        }

        animator.SetBool("close", state);
    }

    public void SetLayer(int layer, int sortingLayer)
    {
        //		GetComponentInChildren<SpriteRenderer>().sortingLayerID = sortingLayer;
        gameObject.layer = layer;
        foreach (Transform child in transform)
        {
            child.gameObject.layer = layer;
        }
    }
    [Server]
    private void DamageOnClose()
    {
        var healthBehaviours = _matrix.Get<HealthBehaviour>(_registerTile.Position);
        foreach (var healthBehaviour in healthBehaviours)
        {
            healthBehaviour.ApplyDamage(gameObject.name, 500, DamageType.BRUTE);
        }
    }

    //Handle network spawn sync failure
    IEnumerator WaitToTryAgain()
    {
        yield return new WaitForSeconds(0.2f);
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                SetState(tempStateCache);
            }
            else
            {
                Debug.LogWarning("ShutterController still failing Animator sync");
            }
        }
        else
        {
            SetState(tempStateCache);
        }
        waitToCheckState = false;
    }
}
