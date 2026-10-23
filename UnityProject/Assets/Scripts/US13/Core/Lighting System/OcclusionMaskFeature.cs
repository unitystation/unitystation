using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace US13.Core.Lighting_System
{
	/// <summary>
	/// URP replacement for Camera.SetReplacementShader on the occlusion mask camera.
	///
	/// Add this ONLY to a dedicated Renderer (second 2D Renderer asset) used by the occlusion camera,
	/// never to the main camera's renderer.
	///
	/// The 2D Renderer still draws the sprites normally first, so this pass clears the color target
	/// and then redraws everything the camera culled using the Fov Mask shader as an override material.
	/// The camera's culling mask (set by OcclusionMaskRenderer) decides which layers are included.
	/// </summary>
	public class OcclusionMaskFeature : ScriptableRendererFeature
	{
		[Tooltip("The 'Custom/Fov Mask' shader (materialContainer.OcclusionMaskShader).")]
		public Shader maskShader;

		private Material mMaterial;
		private OcclusionMaskPass mPass;

		public override void Create()
		{
			if (maskShader == null)
			{
				mPass = null;
				return;
			}

			CoreUtils.Destroy(mMaterial);
			mMaterial = CoreUtils.CreateEngineMaterial(maskShader);

			mPass = new OcclusionMaskPass(mMaterial)
			{
				// Late, but before post processing / the final blit, while the color is still an offscreen texture.
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
			};
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (mPass == null)
				return;

			renderer.EnqueuePass(mPass);
		}

		protected override void Dispose(bool disposing)
		{
			CoreUtils.Destroy(mMaterial);
			mMaterial = null;
		}

		private class OcclusionMaskPass : ScriptableRenderPass
		{
			// Any pass a sprite / tilemap material might have. Untagged passes count as SRPDefaultUnlit.
			private static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>
			{
				new ShaderTagId("Universal2D"),
				new ShaderTagId("UniversalForward"),
				new ShaderTagId("UniversalForwardOnly"),
				new ShaderTagId("SRPDefaultUnlit")
			};

			private readonly Material mMaterial;

			private class PassData
			{
				public RendererListHandle rendererList;
			}

			public OcclusionMaskPass(Material iMaterial)
			{
				mMaterial = iMaterial;
				profilingSampler = new ProfilingSampler("Occlusion Mask");
			}

			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				var _resources = frameData.Get<UniversalResourceData>();
				var _renderingData = frameData.Get<UniversalRenderingData>();
				var _cameraData = frameData.Get<UniversalCameraData>();
				var _lightData = frameData.Get<UniversalLightData>();

				if (_resources.activeColorTexture.IsValid() == false) return;

				var _drawing = RenderingUtils.CreateDrawingSettings(ShaderTags, _renderingData, _cameraData, _lightData, SortingCriteria.CommonTransparent);
				_drawing.overrideMaterial = mMaterial;
				_drawing.overrideMaterialPassIndex = 0;

				var _filtering = new FilteringSettings(RenderQueueRange.all, -1);
				var _listParams = new RendererListParams(_renderingData.cullResults, _drawing, _filtering);

				using (var _builder = renderGraph.AddRasterRenderPass<PassData>("Occlusion Mask", out var _data))
				{
					//this shits fucking stupid kill me
					_data.rendererList = renderGraph.CreateRendererList(_listParams);

					_builder.UseRendererList(_data.rendererList);
					_builder.SetRenderAttachment(_resources.activeColorTexture, 0);
					_builder.AllowPassCulling(false);

					_builder.SetRenderFunc((PassData iData, RasterGraphContext iContext) =>
					{
						// Throw away the normally rendered sprites; only the mask should remain.
						iContext.cmd.ClearRenderTarget(false, true, Color.clear);
						iContext.cmd.DrawRendererList(iData.rendererList);
					});
				}
			}
		}
	}
}