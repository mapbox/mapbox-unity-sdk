Shader "Custom/TextureMask" {
	Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        _BuildingOneMaskTex ("BuildingOneMask", 2D) = "white" {}
        _BuildingTwoMaskTex ("BuildingTwoMask", 2D) = "white" {}

        _WindowMaskTex ("WindowMask", 2D) = "white" {}

        _BuildingOneColor ("BuildingOneColor", Color) = (1,1,1,1)
        _BuildingTwoColor ("BuildingTwoColor", Color) = (1,1,1,1)
        _BuildingThreeColor ("BuildingThreeColor", Color) = (1,1,1,1)
        _BuildingFourColor ("BuildingFourColor", Color) = (1,1,1,1)

        _WindowColor ("WindowColor", Color) = (1,1,1,1)

		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex, _BuildingOneMaskTex, _BuildingTwoMaskTex, _WindowMaskTex;

		struct Input {
			float2 uv_MainTex, uv_BuildingOneMaskTex, uv_BuildingTwoMaskTex, uv_WindowMaskTex;
		};

		half _Glossiness;
		half _Metallic;
        
		fixed4 _BuildingOneColor;
        fixed4 _BuildingTwoColor;
        fixed4 _BuildingThreeColor;
        fixed4 _BuildingFourColor;

        fixed4 _WindowColor;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            
            float b1_mask = tex2D (_BuildingOneMaskTex, IN.uv_BuildingOneMaskTex).r;
            float b2_mask = tex2D (_BuildingTwoMaskTex, IN.uv_BuildingTwoMaskTex).r;
            float b3_mask = tex2D (_BuildingOneMaskTex, IN.uv_BuildingOneMaskTex).b;
            float b4_mask = tex2D (_BuildingTwoMaskTex, IN.uv_BuildingTwoMaskTex).b;


            float w_mask = tex2D (_WindowMaskTex, IN.uv_WindowMaskTex).r;

            c.rgb = c.rgb * (1 - b1_mask) + _BuildingOneColor * b1_mask;
            c.rgb = c.rgb * (1 - b2_mask) + _BuildingTwoColor * b2_mask;
            c.rgb = c.rgb * (1 - b3_mask) + _BuildingThreeColor * b3_mask;
            c.rgb = c.rgb * (1 - b4_mask) + _BuildingFourColor * b4_mask;

            c.rgb = c.rgb * (1 - w_mask) + _WindowColor * w_mask;

			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
