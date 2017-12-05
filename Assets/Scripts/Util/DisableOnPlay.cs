using UnityEngine;
public class DisableOnPlay : MonoBehaviour
{
    void Start()
    {
        gameObject.SetActive(false);
    }
}
