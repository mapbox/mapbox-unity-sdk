using UnityEngine;

namespace UnityARInterface
{
    public class ARLightEstimate : MonoBehaviour
    {
        private Light m_Light;

        // Use this for initialization
        void Start()
        {
            m_Light = GetComponent<Light>();

            if (m_Light == null)
                enabled = false;
        }

        void Update()
        {
            var lightEstimate = ARInterface.GetInterface().GetLightEstimate();

            if ((lightEstimate.capabilities & ARInterface.LightEstimateCapabilities.AmbientIntensity) == ARInterface.LightEstimateCapabilities.AmbientIntensity)
                m_Light.intensity = lightEstimate.ambientIntensity;

            if ((lightEstimate.capabilities & ARInterface.LightEstimateCapabilities.AmbientColorTemperature) == ARInterface.LightEstimateCapabilities.AmbientColorTemperature)
                m_Light.colorTemperature = lightEstimate.ambientColorTemperature;
        }
    }
}
