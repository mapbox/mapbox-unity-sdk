// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Mapbox/Clip_MapboxStyles"
{
	Properties
	{
		_BaseColor ("BaseColor", Color) = (1,1,1,1)
		_DetailColor1 ("DetailColor1", Color) = (1,1,1,1)
		_DetailColor2 ("DetailColor2", Color) = (1,1,1,1)

		_BaseTex ("Base", 2D) = "white" {}
		_DetailTex1 ("Detail_1", 2D) = "white" {}
		_DetailTex2 ("Detail_2", 2D) = "white" {}

		_Emission ("Emission", Range(0.0, 1.0)) = 0.1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		float4 _BaseColor;
		float4 _DetailColor1;
		float4 _DetailColor2;

		sampler2D _BaseTex;
		sampler2D _DetailTex1;
		sampler2D _DetailTex2;

		float _Emission;

		float3 _Origin;
		float3 _BoxSize;
		float3 _BoxRotation;

		struct Input
		{
			float2 uv_BaseTex, uv_DetailTex1, uv_DetailTex2;
			float3 worldPos;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o)
		{

			float3 dir = IN.worldPos - _Origin;
			float3 rads = float3(radians(_BoxRotation.x), radians(_BoxRotation.y), radians(_BoxRotation.z));
			// z
			dir = cos(rads.z) * dir + sin(rads.z) * cross(float3(0,0,1.0f), dir) + (1.0f - cos(rads.z)) * dot(float3(0,0,1.0f), dir) * float3(0,0,1.0f);
			// x
			dir = cos(rads.x) * dir + sin(rads.x) * cross(float3(1.0f,0,0), dir) + (1.0f - cos(rads.x)) * dot(float3(1.0f,0,0), dir) * float3(1.0f,0,0);
			// y
			dir = cos(rads.y) * dir + sin(rads.y) * cross(float3(0,1.0f,0), dir) + (1.0f - cos(rads.y)) * dot(float3(0,1.0f,0), dir) * float3(0,1.0f,0);
			
			half3 dist = half3(
				abs(dir.x), // no negatives
				abs(dir.y), // no negatives
				abs(dir.z)  // no negatives
			);

			dist.x = dist.x - _BoxSize.x * 0.5;
			dist.y = dist.y - _BoxSize.y * 0.5;
			dist.z = dist.z - _BoxSize.z * 0.5;

			half t = min(1, dist.x);
			t = max(t, dist.y);
			t = max(t, dist.z);
			
			clip(-1 * t);

			fixed4 baseTexture = tex2D (_BaseTex, IN.uv_BaseTex);

			fixed4 detailTexture1 = tex2D (_DetailTex1, IN.uv_DetailTex1);
			fixed4 detailTexture2 = tex2D (_DetailTex2, IN.uv_DetailTex2);

			fixed4 baseDetail1_Result = lerp(_BaseColor, _DetailColor1, detailTexture1.a);

			fixed4 detail1Detail2_Result  = lerp(baseDetail1_Result, _DetailColor2, detailTexture2.a);

			fixed4 c = baseTexture *= detail1Detail2_Result;
			half3 e = c.rgb;

			o.Albedo = c.rgb;
			o.Emission = e * _Emission;
			o.Alpha = 1.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
