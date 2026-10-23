using UnityEngine;
using US13.Managers.UpdateManager;

namespace US13.Core.Camera
{
    public class HighCamera : CameraEffectBase
    {
        public float _HueSpeed;
        public float _HueIntensity;
        public float _PlasmaSpeed;
        public float _PlasmaScale;
        public float _PlasmaBlend;
        public float _DistortionAmount;
        public float _DistortTimeSpeed;

        public Material material;

        private float huePhase;
        private float plasmaPhase;
        private float distortPhase;
        private float targetDistortionAmount;

        public void SetStrength(float Strength)
        {
	        //max =
	        //_HueSpeed = 0.5
	        //_HueIntensity = 1
	        //_PlasmaSpeed = 1.25
	        //_PlasmaScale = 0.5
	        //_PlasmaBlend = 1
	        //_DistortionAmount = 0.04
	        //_DistortTimeSpeed = 1
            if (Strength > 0.75f)
            {
                var CorrectedStrength = (Strength - 0.75f) * 4;
                _HueSpeed = Mathf.Lerp(0.1f, 0.5f, CorrectedStrength);
                _HueIntensity = 1;
                _PlasmaSpeed = 1.25f;
                _PlasmaScale = 0.5f;
                _PlasmaBlend = Mathf.Lerp(0.295f, 1f, CorrectedStrength);
                targetDistortionAmount = Mathf.Lerp(0.02f, 0.04f, CorrectedStrength);
                _DistortTimeSpeed = Mathf.Lerp(0.1f, 1f, CorrectedStrength);
                this.enabled = true;
            }
            else if (Strength > 0.55f)
            {
                _HueSpeed = 0.1f;
                _HueIntensity = 1f;
                _PlasmaSpeed = 1.25f;
                _PlasmaScale = 0.5f;
                _PlasmaBlend = 0.295f;
                targetDistortionAmount = 0.02f;
                _DistortTimeSpeed = 0.1f;
                this.enabled = true;
            }
            else if (Strength > 0.35f)
            {
                var CorrectedStrength = (Strength - 0.35f) * 5f;
                _HueSpeed = 0.1f;
                _HueIntensity = Mathf.Lerp(0.25f, 1, CorrectedStrength);
                _PlasmaSpeed = Mathf.Lerp(1f, 1.25f, CorrectedStrength);
                _PlasmaScale = 0.5f;
                _PlasmaBlend = Mathf.Lerp(0.125f, 0.295f, CorrectedStrength);
                targetDistortionAmount = Mathf.Lerp(0f, 0.02f, CorrectedStrength);
                _DistortTimeSpeed = Mathf.Lerp(0.5f, 0.1f, CorrectedStrength);
                this.enabled = true;
            }
            else if (Strength > 0.10)
            {
                _HueSpeed = 0.1f;
                _HueIntensity = 0.25f;
                _PlasmaSpeed = 1f;
                _PlasmaScale = 0.5f;
                _PlasmaBlend = 0.125f;
                targetDistortionAmount = 0f;
                _DistortTimeSpeed = 0.5f;
                this.enabled = true;
            }
            else if (Strength > 0)
            {
                var CorrectedStrength = Strength * 10f;
                _HueSpeed = Mathf.Lerp(0, 0.1f, CorrectedStrength);
                _HueIntensity = Mathf.Lerp(0f, 0.25f, CorrectedStrength);
                _PlasmaSpeed = Mathf.Lerp(0, 1f, CorrectedStrength);
                _PlasmaScale = 0.5f;
                _PlasmaBlend = Mathf.Lerp(0f, 0.125f, CorrectedStrength);
                targetDistortionAmount = 0;
                _DistortTimeSpeed = Mathf.Lerp(0, 0.5f, CorrectedStrength);
                this.enabled = true;
            }
            else
            {
                this.enabled = false;
            }
        }

        public void OnEnable()  => UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
        public void OnDisable() => UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);

        void UpdateMe()
        {
            _DistortionAmount = Mathf.Lerp(_DistortionAmount, targetDistortionAmount, Time.deltaTime * 8);

            huePhase     += Time.deltaTime * _HueSpeed;
            plasmaPhase  += Time.deltaTime * _PlasmaSpeed;
            distortPhase += Time.deltaTime * _DistortTimeSpeed;

            if (huePhase > 100000) huePhase = 0;
            if (plasmaPhase > 100000) plasmaPhase = 0;
            if (distortPhase > 100000) distortPhase = 0;
        }

        public override Material GetMaterial()
        {
            var mat = GetInstance(material);
            if (mat == null) return null;

            mat.SetFloat("_HueSpeed", _HueSpeed);
            mat.SetFloat("_HueIntensity", _HueIntensity);
            mat.SetFloat("_PlasmaSpeed", _PlasmaSpeed);
            mat.SetFloat("_PlasmaScale", _PlasmaScale);
            mat.SetFloat("_PlasmaBlend", _PlasmaBlend);
            mat.SetFloat("_DistortionAmount", _DistortionAmount);
            mat.SetFloat("_DistortTimeSpeed", _DistortTimeSpeed);
            mat.SetFloat("_HuePhase", huePhase);
            mat.SetFloat("_PlasmaPhase", plasmaPhase);
            mat.SetFloat("_DistortPhase", distortPhase);
            return mat;
        }
    }
}