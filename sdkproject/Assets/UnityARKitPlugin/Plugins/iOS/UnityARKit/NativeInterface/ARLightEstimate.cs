using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.iOS
{
	public struct ARLightEstimate
	{
		public float ambientIntensity;
	}

	[Serializable]
	public struct UnityARLightEstimate
	{
		public float ambientIntensity;
		public float ambientColorTemperature;

		public UnityARLightEstimate(float intensity, float temperature)
		{
			ambientIntensity = intensity;
			ambientColorTemperature = temperature;
		}
	};


	public struct MarshalDirectionalLightEstimate
	{
		public Vector4 primaryDirAndIntensity;
		public IntPtr  sphericalHarmonicCoefficientsPtr;

		public float [] SphericalHarmonicCoefficients { get { return MarshalCoefficients(sphericalHarmonicCoefficientsPtr); } } 

		float [] MarshalCoefficients(IntPtr ptrFloats)
		{
			int numFloats = 27;
			float [] workCoefficients = new float[numFloats];
			Marshal.Copy (ptrFloats, workCoefficients, 0, (int)(numFloats)); 
			RotateForUnity (ref workCoefficients, 0);
			RotateForUnity (ref workCoefficients, 9);
			RotateForUnity (ref workCoefficients, 18);
			return workCoefficients;

		}

		void RotateForUnity(ref float[] shc, int startIndex)
		{
			//from observation, it looks like top, bottom and left,right are flipped (x,y)

			// we got this info:
			// Lets assume Unity uses the same convention for spherical harmonics (yzx) wrt its own coordinate system as scenekit. 
			// (This could be another source of error).
			// To tackle a constant transform between the unity and scenekit world coordinate system conventions:

			// Axis flips work as follows for the 9 components of the sh vector:

			//	0-component: no change

			//	1-component: negate if y is supposed to be flipped
			//	2-component: negate if z is supposed to be flipped
			//	3-component: negate if x is supposed to be flipped

			//	4-component: negate if x or y are supposed to be flipped, but not both!
			//	5-component: negate if y or z are supposed to be flipped, but not both!
			//	6-component: no change
			//	7-component: negate if x or z are supposed to be flipped, but not both!
			//	8-component: no change

			shc [startIndex+1] = -shc [startIndex+1];
			shc [startIndex+3] = -shc [startIndex+3];

			shc [startIndex + 5] = -shc [startIndex + 5];				
			shc [startIndex + 7] = -shc [startIndex + 7];
				

		}
	}

	public class UnityARDirectionalLightEstimate
	{
		public Vector3 primaryLightDirection;
		public float primaryLightIntensity;
		public float [] sphericalHarmonicsCoefficients;

		public UnityARDirectionalLightEstimate (float [] SHC, Vector3 direction, float intensity)
		{
			sphericalHarmonicsCoefficients = SHC;
			primaryLightDirection = direction;
			primaryLightIntensity = intensity;
		}
	};

	public enum LightDataType
	{
		LightEstimate,
		DirectionalLightEstimate
	}

	public struct UnityMarshalLightData
	{
		public LightDataType arLightingType;
		public UnityARLightEstimate arLightEstimate;
		public MarshalDirectionalLightEstimate arDirectonalLightEstimate;

		public UnityMarshalLightData(LightDataType ldt, UnityARLightEstimate ule, MarshalDirectionalLightEstimate mdle)
		{
			arLightingType = ldt;
			arLightEstimate = ule;
			arDirectonalLightEstimate = mdle;
		}

		public static implicit operator UnityARLightData(UnityMarshalLightData rValue)
		{
			UnityARDirectionalLightEstimate udle = null;
			if (rValue.arLightingType == LightDataType.DirectionalLightEstimate) {
				Vector4 lightDirAndIntensity = rValue.arDirectonalLightEstimate.primaryDirAndIntensity;
				Vector3 lightDir = new Vector3 (lightDirAndIntensity.x, lightDirAndIntensity.y, lightDirAndIntensity.z);
				float[] shc = rValue.arDirectonalLightEstimate.SphericalHarmonicCoefficients;
				udle = new UnityARDirectionalLightEstimate (shc, lightDir, lightDirAndIntensity.w);
			}
			return new UnityARLightData(rValue.arLightingType, rValue.arLightEstimate, udle);
		}

	}

	public struct UnityARLightData
	{
		public LightDataType arLightingType;
		public UnityARLightEstimate arLightEstimate;
		public UnityARDirectionalLightEstimate arDirectonalLightEstimate;

		public UnityARLightData(LightDataType ldt, UnityARLightEstimate ule, UnityARDirectionalLightEstimate udle)
		{
			arLightingType = ldt;
			arLightEstimate = ule;
			arDirectonalLightEstimate = udle;
		}


	}


}

