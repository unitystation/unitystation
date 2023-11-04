using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LightEmissionBehaviour : MonoBehaviour
{
	private const int LightSourceLayer = 21;
	private const string EmissionShaderName = "Stencil/Unlit background masked emission";

	private static Material mEmissionMaterial;

	[SerializeField]
	[Range(0, 1)]
	[Tooltip("Controls how much of sprite color bleeds in to light mask.")]
	private float mIntensity = 0.25f;

	[SerializeField]
	[Range(0, 1)]
	[Tooltip("Additional flat white color helps lit black sprites, if required.")]
	private float mAdditionalEmission = 0;

	[SerializeField]
	[Tooltip("Scaling option to control glow size.")]
	private float mScale = 1;

	private SpriteRenderer mParentRenderer;
	private SpriteRenderer mEmissionRenderer;
	private GameObject mEmissionGO;
	private Color mEmissionParameters = Color.black;

	private static Material emissionMaterial
	{
		get
		{
			if (mEmissionMaterial == null)
			{
				mEmissionMaterial = new Material(Shader.Find(EmissionShaderName))
					                    {
						                    name = "Generated Emission Material"
					                    };
			}

			return mEmissionMaterial;
		}
	}

	/// <summary>
	/// Use sprite renderers color channel to pass parameters in to material.
	/// Assuming we don't need to use color in generic way, this allows us to not bother with injecting additional
	/// stream in to sprite mesh.
	/// </summary>
	private Color emissionParameters
	{
		get
		{
			return mEmissionParameters;
		}

		set
		{
			if (mEmissionParameters == value)
				return;

			mEmissionParameters = value;

			mEmissionRenderer.color = value;
		}
	}

	private void OnEnable()
	{
		mParentRenderer = gameObject.GetComponent<SpriteRenderer>();

		if (mEmissionGO == null || mEmissionRenderer == null)
		{
			InitializeRenderer(gameObject, emissionMaterial, out mEmissionGO, out mEmissionRenderer);
		}

		mEmissionGO?.SetActive(true);
	}

	private void OnDisable()
	{
		mEmissionGO?.SetActive(false);
	}

	/// <summary>
	/// Trigger synchronization of rendering parameters.
	/// Using OnWillRenderObject will insure that synchronization happens only when necessary.
	/// </summary>
	private void OnWillRenderObject()
	{
		SynchronizeRenderers();
	}

	private void SynchronizeRenderers()
	{
		// WallmountSpriteBehavior hides wallmounts by setting alpha to 0. We need to track that and update emission in case it does.
		bool _parentIsDimmed = mParentRenderer.color.a < 0.1f;

		mEmissionRenderer.enabled = mParentRenderer.enabled && _parentIsDimmed == false;

		if (mEmissionRenderer.enabled == false)
		{
			return;
		}

		mEmissionRenderer.sprite = mParentRenderer.sprite;
		mEmissionRenderer.size = mParentRenderer.size;

		mEmissionGO.transform.localScale = Vector3.one * mScale;

		emissionParameters = new Color(mIntensity, mAdditionalEmission, 0, 0);
	}

	private static void InitializeRenderer(GameObject iRoot, Material iEmissionMaterial, out GameObject oGameObject, out SpriteRenderer oSpriteRenderer)
	{
		oGameObject = null;
		oSpriteRenderer = null;

		if (iRoot == null)
		{
			Loggy.LogError("LightEmissionBehaviour: UInitialization require valid parent.", Category.Lighting);
			return;
		}

		if (iEmissionMaterial == null)
		{
			Loggy.LogError("LightEmissionBehaviour: UInitialization require assigned emission material.", Category.Lighting);
			return;
		}

		oGameObject = new GameObject("Emission Renderer");
		oGameObject.transform.parent = iRoot.transform;
		oGameObject.transform.localPosition = Vector3.zero;
		oGameObject.transform.localEulerAngles = Vector3.zero;
		oGameObject.transform.localScale = Vector3.one;
		oGameObject.layer = LightSourceLayer;

		oSpriteRenderer = oGameObject.AddComponent<SpriteRenderer>();
		oSpriteRenderer.material = iEmissionMaterial;
	}
}