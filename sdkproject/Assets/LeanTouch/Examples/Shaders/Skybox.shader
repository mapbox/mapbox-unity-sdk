Shader "Lean/Skybox"
{
	Properties
	{
		_Color1("Color 1", Color) = (1.0, 0.5, 0.5)
		_Color2("Color 2", Color) = (0.5, 0.5, 1.0)
	}
	SubShader
	{
		Tags
		{
			"Queue"       = "Background"
			"RenderType"  = "Background"
			"PreviewType" = "Skybox"
		}

		CGPROGRAM
		#pragma surface Surf NoLighting

		float3 _Color1;
		float3 _Color2;

		struct Input
		{
			float4 screenPos;
		};

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
		{
			return fixed4(s.Albedo, s.Alpha);
		}

		void Surf(Input IN, inout SurfaceOutput o)
		{
			float2 coord = IN.screenPos.xy / IN.screenPos.w - 0.5f;

			o.Albedo = lerp(_Color1, _Color2, length(coord));
		}
		ENDCG
	} // SubShader
} // Shader