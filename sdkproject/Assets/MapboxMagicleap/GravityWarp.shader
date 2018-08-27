Shader "Unlit/GravityWarp"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { 
			"RenderType"="Opaque" 
			"DisableBatching"="True"
			}


		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float sphereMask :TEXCOORD1;
				float3 posWorld : TEXCOORD2;
				float3 normal : NORMAL;
			};

			uniform sampler2D _MainTex;
			uniform float3 _GravityPoint;
			uniform float _GravityMulitplier; float _GravityDistance;
			uniform float3 _ClipPoint;
			uniform float _ClipDistance;
			
			v2f vert (appdata v)
			{
				v2f o; 
				float3 localGravityPoint = mul(unity_WorldToObject, _GravityPoint).rgb;
				o.posWorld  = mul(unity_ObjectToWorld, v.vertex).xyz;

				float3 dir = normalize(o.posWorld - _GravityPoint);
				float dist = distance(o.posWorld, _GravityPoint);
				float saturatedDist = saturate(1 - (dist * _GravityDistance));

				float3 appliedVector = dir * saturatedDist * _GravityMulitplier;
				float3 worldAppliedVector = mul(unity_ObjectToWorld, appliedVector);
				if(length(worldAppliedVector) > dist && _GravityMulitplier < 0){
					v.vertex.rgb = localGravityPoint;
				}
				else
				{
					v.vertex.rgb += appliedVector;
				}

				// TODO clamp to center point

				o.sphereMask = distance(o.posWorld + worldAppliedVector, _ClipPoint);
	
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.normal = v.normal;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D (_MainTex, i.uv);
				//c = lerp(c, tex2D (_MainTex, i.posWorld.yz), i.normal.x * i.normal.x) ;
				//c = lerp(c, tex2D (_MainTex, i.posWorld.xz), i.normal.y * i.normal.y) ;

				c = lerp(c, c + c * 0.3, saturate((i.sphereMask * i.sphereMask * i.sphereMask) / (_ClipDistance * 4)));
				if(i.sphereMask > _ClipDistance)
				{
					discard;
				}

				return c;
			}
			ENDCG
		}
	}
}