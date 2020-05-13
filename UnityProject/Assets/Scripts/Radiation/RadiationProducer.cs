using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Light2D;

public class RadiationProducer : MonoBehaviour
{
	public float OutPuttingRadiation = 0;
	public Color color = new Color();
	private GameObject mLightRendererObject;

	private void Awake()
	{

		if (mLightRendererObject == null)
		{
			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, color, 7);
			mLightRendererObject.SetActive(false);
		}

	}

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
