// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Mapbox/MapboxStylesPerRenderer" {
    Properties 
    {
        [PerRendererData]_BaseColor ("BaseColor", Color) = (1,1,1,1)
        [PerRendererData]_DetailColor1 ("DetailColor1", Color) = (1,1,1,1)
        [PerRendererData]_DetailColor2 ("DetailColor2", Color) = (1,1,1,1)

        _BaseTex ("Base", 2D) = "white" {}
        _DetailTex1 ("Detail_1", 2D) = "white" {}
        _DetailTex2 ("Detail_2", 2D) = "white" {}

        _Emission ("Emission", Range(0.0, 1.0)) = 0.1
    }
    SubShader 
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
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

        struct Input 
        {
            float2 uv_BaseTex, uv_DetailTex1, uv_DetailTex2;
        };
        
        

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o) 
        {
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
