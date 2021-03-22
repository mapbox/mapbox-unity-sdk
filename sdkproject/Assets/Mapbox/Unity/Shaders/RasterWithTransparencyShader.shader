// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Mapbox/Raster With Transparency" 
{
	Properties 
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "blue" {}
		_Alpha ("Alpha", Float) = 1
	}

	Category
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off Lighting Off ZWrite Off

		SubShader 
		{		
			Pass
			{
				BindChannels 
				{
					Bind "Color", color
					Bind "Vertex", vertex
					Bind "TexCoord", texcoord
				}

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
			    #pragma multi_compile_particles
				#pragma fragmentoption ARB_precision_hint_fastest

				#include "UnityCG.cginc"

				// Use shader model 3.0 target, to get nicer looking lighting
				#pragma target 3.0

				sampler2D _MainTex;
				float _Alpha;
				float4 _MainTex_ST;
								
				fixed4 _Color;

				struct vertexIn
				{
					float4 vertex			: POSITION;
					float4 texcoord			: TEXCOORD0;
					fixed4 color			: COLOR;
				};
			
				struct vertexOut 
				{
					float4 pos				: SV_POSITION;
					float2 uv				: UV_COORD0;
					float4 color			: COLOR;
					float2 screenspaceUv	: UV_COORD1;
				};

				vertexOut vert(vertexIn v)
				{
					vertexOut o;
					o.pos = UnityObjectToClipPos (v.vertex);
					o.color = v.color;
					o.uv = v.texcoord.xy;
					o.screenspaceUv = o.pos.xy;
					return o;
				}

				fixed4 frag(vertexOut o) : SV_Target
				{
					float2 uv = TRANSFORM_TEX(o.uv, _MainTex);
					fixed4 texColor = tex2D(_MainTex, uv);
					
					fixed4 color = o.color * texColor;
					// check how much color used for transparency equals c.rgb in range 0..1
					// 1=no match, 0=full match
					fixed3 delta = texColor.rgb - _Color.rgb;
					float match = dot(delta,delta);
                    float threshold = step(match,0.1);

					color.a =  (1 - ((threshold) * (1 -_Color.a))) * _Alpha;

					return color;
				}
		 
				ENDCG
			}
		}
	} 
	FallBack "Diffuse"
}