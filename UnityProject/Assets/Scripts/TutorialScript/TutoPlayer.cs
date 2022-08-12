using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutoPlayer : MonoBehaviour
{
    [SerializeField] private GameObject playerClothes;
    [SerializeField] private float waitTimeCleaningClothes;
    private PlayerScript playerScript;
    /// Start is called before the first frame update
    void Start()
    {
        //destroy itself when not on tutorial
        if(!GameManager.Instance.onTuto)
        {
            Destroy(this);
        }
        else
        {
            ///make player rigibody dynamic for trigger
            ///remove all its clothes and items currently having when spawning
            this.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;            
            playerScript = this.GetComponent<PlayerScript>();
            StartCoroutine("RemoveAllClothes");
        }
    }

    private void FixedUpdate()
    {
        //can't die on tutorial
        if(playerScript.playerHealth.IsDead || playerScript.playerHealth.IsCrit)
        {
            if(GameManager.Instance.onTuto)
                playerScript.playerHealth.FullyHeal();
        }
    }

    ///remove all clothes and item
    private IEnumerator RemoveAllClothes()
    {
        yield return WaitFor.Seconds(waitTimeCleaningClothes);

        UI_DynamicItemSlot[] dynamicItemSlots = UIManager.Instance.GetComponentsInChildren<UI_DynamicItemSlot>();
        for(int j = 0; j < dynamicItemSlots.Length; j++)
        {
            Despawn.ServerSingle(dynamicItemSlots[j].ItemObject, false);
            dynamicItemSlots[j].Reset();
        }
        
        UI_DynamicItemSlot[] clothesItemSlots = UIManager.Instance.GetComponentInChildren<ControlClothing>().ObjectToHide.GetComponentsInChildren<UI_DynamicItemSlot>();
        for(int j = 0; j < clothesItemSlots.Length; j++)
        {
            Despawn.ServerSingle(clothesItemSlots[j].ItemObject, false);
            clothesItemSlots[j].Reset();
            
        }

        SpriteHandler[] allPlayerClothes = playerClothes.GetComponentsInChildren<SpriteHandler>();
        for(int i = 0; i < allPlayerClothes.Length; i++)
        {
            if(allPlayerClothes[i].gameObject.name != "Underwear")
            {
                allPlayerClothes[i].Empty(false);
                allPlayerClothes[i].GetComponent<SpriteRenderer>().sprite = null;
            }
        }

    }
}
