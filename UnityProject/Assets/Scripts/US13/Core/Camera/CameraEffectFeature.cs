using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace US13.Core.Camera
{
    public class CameraEffectsFeature : ScriptableRendererFeature
    {
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

        private CameraEffectsPass _pass;
        private readonly List<CameraEffectBase> _scratch = new();

        public override void Create()
        {
            _pass = new CameraEffectsPass { renderPassEvent = injectionPoint };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camData = renderingData.cameraData;
            if (camData.cameraType != CameraType.Game) return;

            camData.camera.GetComponents(_scratch);
            var list = _pass.Effects;
            list.Clear();
            foreach (var e in _scratch)
                if (e.isActiveAndEnabled) list.Add(e);

            if (list.Count == 0) return;
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            renderer.EnqueuePass(_pass);
        }

        private sealed class CameraEffectsPass : ScriptableRenderPass
        {
            public readonly List<CameraEffectBase> Effects = new();

            public CameraEffectsPass()
            {
                requiresIntermediateTexture = true; // we can't sample the backbuffer
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;

                foreach (var effect in Effects)
                {
                    var material = effect.GetMaterial();
                    if (material == null) continue;

                    var source = resources.activeColorTexture;
                    var desc = renderGraph.GetTextureDesc(source);
                    desc.name = "_CameraEffect_" + effect.GetType().Name;
                    desc.clearBuffer = false;
                    var dest = renderGraph.CreateTexture(desc);

                    renderGraph.AddBlitPass(
                        new RenderGraphUtils.BlitMaterialParameters(source, dest, material, effect.PassIndex),
                        passName: "CameraEffect " + effect.GetType().Name);

                    resources.cameraColor = dest; // next effect reads from here
                }
            }
        }
    }
}