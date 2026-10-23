using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace US13.Core.Lighting_System
{
	/// <summary>
	/// blits the camera color through the lighting material (and optionally the UI material).
	/// All textures and material properties are prepared by
	/// LightingSystem.LateUpdate; this pass only does the blits.
	/// </summary>
	public class LightingRendererFeature : ScriptableRendererFeature
	{
		private LightingPass mPass;

		public override void Create()
		{
			mPass = new LightingPass
			{
				// Must run before the final blit to the screen: after post processing the 2D renderer
				// is already writing to the back buffer, which can't be sampled.
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
			};

			// We sample the camera color, so it can't be the back buffer.
			mPass.requiresIntermediateTexture = true;
			mPass.ConfigureInput(ScriptableRenderPassInput.Color);
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			var _camera = renderingData.cameraData.camera;

			if (!_camera.TryGetComponent(out LightingSystem _system) || !_system.isActiveAndEnabled)
				return;

			if (_system.compositeMode == LightingSystem.CompositeMode.None)
				return;

			mPass.system = _system;
			renderer.EnqueuePass(mPass);
		}

		private class LightingPass : ScriptableRenderPass
		{
			public LightingSystem system;

			private bool mWarnedBackBuffer;

			private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

			public LightingPass()
			{
				profilingSampler = new ProfilingSampler("Lighting Composite");
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				if (system == null)
					return;

				var _resources = frameData.Get<UniversalResourceData>();
				// This looks unused, but it's not. Don't fucking touch this
				var _cameraData = frameData.Get<UniversalCameraData>();

				// Only the final camera of a stack output matters; skip cameras with no usable color.
				if (_resources.activeColorTexture.IsValid() == false) return;

				// A back buffer has no descriptor and can't be sampled; bail out instead of throwing,
				// because an exception here corrupts the whole render graph for the frame.
				if (_resources.isActiveTargetBackBuffer)
				{
					if (!mWarnedBackBuffer)
					{
						Debug.LogWarning("LightingRendererFeature: camera target is the back buffer, so the lighting composite is skipped. " +
						                 "Make sure the pass runs before post processing and the camera uses an intermediate texture.");
						mWarnedBackBuffer = true;
					}
					return;
				}

				TextureHandle _color = _resources.activeColorTexture;

				switch (system.compositeMode)
				{
					case LightingSystem.CompositeMode.Lit:
						{
							var _material = system.compositeMaterial;
							if (_material == null)
								return;

							BlitThroughMaterial(renderGraph, _color, _material, "Lighting Mix");

							if (system.compositeUI && system.uiCompositeMaterial != null)
							{
								BlitThroughMaterial(renderGraph, _color, system.uiCompositeMaterial, "Lighting UI");
							}
							break;
						}

					case LightingSystem.CompositeMode.Debug:
						{
							if (system.DebugHandle == null)
								return;

							TextureHandle _debug = renderGraph.ImportTexture(system.DebugHandle);
							renderGraph.AddBlitPass(_debug, _color, Vector2.one, Vector2.zero, passName: "Lighting Debug View");
							break;
						}
				}
			}

			/// <summary>
			/// Can't read and write the same texture, so: copy color -> temp, then temp -> color through the material.
			/// The old shaders sampled _MainTex (set by Graphics.Blit), so the source is bound to that property.
			/// </summary>
			private static void BlitThroughMaterial(RenderGraph renderGraph, TextureHandle iColor, Material iMaterial, string iName)
			{
				var _desc = renderGraph.GetTextureDesc(iColor);
				_desc.name = "_LightingSourceCopy";
				_desc.clearBuffer = false;
				TextureHandle _copy = renderGraph.CreateTexture(_desc);

				renderGraph.AddCopyPass(iColor, _copy, iName + " Copy");

				var _parameters = new RenderGraphUtils.BlitMaterialParameters(_copy, iColor, iMaterial, 0);
				_parameters.sourceTexturePropertyID = MainTexId;
				renderGraph.AddBlitPass(_parameters, iName);
			}
		}
	}
}