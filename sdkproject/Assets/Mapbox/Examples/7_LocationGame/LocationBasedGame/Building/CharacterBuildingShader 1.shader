// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CharacterBuildingShader"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
		_Texture("Texture", 2D) = "white" {}
		_EmissionColor("EmissionColor", Color) = (0,0,0,0)
		_EmissionPower("EmissionPower", Float) = 0
		_Color("Color", Color) = (0,0,0,0)
		_CharacterPosition("CharacterPosition", Vector) = (0,0,0,0)
		_Range("Range", Float) = 0
		_Tile("Tile", Float) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform float4 _Color;
		uniform sampler2D _Texture;
		uniform float _Tile;
		uniform float4 _EmissionColor;
		uniform float3 _CharacterPosition;
		uniform float _Range;
		uniform float _EmissionPower;


		inline float4 TriplanarSampling( sampler2D topTexMap, sampler2D midTexMap, sampler2D botTexMap, float3 worldPos, float3 worldNormal, float falloff, float tilling, float vertex )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= projNormal.x + projNormal.y + projNormal.z;
			float3 nsign = sign( worldNormal );
			half4 xNorm; half4 yNorm; half4 zNorm;
			if(vertex == 1){
			xNorm = ( tex2Dlod( topTexMap, float4((tilling * worldPos.zy * float2( nsign.x, 1.0 )).xy,0,0) ) );
			yNorm = ( tex2Dlod( topTexMap, float4((tilling * worldPos.zx).xy,0,0) ) );
			zNorm = ( tex2Dlod( topTexMap, float4((tilling * worldPos.xy * float2( -nsign.z, 1.0 )).xy,0,0) ) );
			} else {
			xNorm = ( tex2D( topTexMap, tilling * worldPos.zy * float2( nsign.x, 1.0 ) ) );
			yNorm = ( tex2D( topTexMap, tilling * worldPos.zx ) );
			zNorm = ( tex2D( topTexMap, tilling * worldPos.xy * float2( -nsign.z, 1.0 ) ) );
			}
			return xNorm* projNormal.x + yNorm* projNormal.y + zNorm* projNormal.z;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex);
			float2 componentMask36 = ase_worldPos.xz;
			float2 componentMask37 = _CharacterPosition.xz;
			float clampResult16 = clamp( (0.0 + (pow( distance( componentMask36 , componentMask37 ) , 3.0 ) - 0.0) * (1.0 - 0.0) / (pow( _Range , 3.0 ) - 0.0)) , 0.0 , 1.0 );
			float temp_output_20_0 = ( 1.0 - clampResult16 );
			float lerpResult19 = lerp( 0.0 , ase_worldPos.y , temp_output_20_0);
			float4 appendResult17 = (float4(0.0 , ( lerpResult19 * -0.94 ) , 0.0 , 0.0));
			v.vertex.xyz += appendResult17.xyz;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float4 triplanar41 = TriplanarSampling( _Texture, _Texture, _Texture, ase_worldPos, ase_worldNormal, 1.0, _Tile, 0 );
			float2 componentMask36 = ase_worldPos.xz;
			float2 componentMask37 = _CharacterPosition.xz;
			float clampResult16 = clamp( (0.0 + (pow( distance( componentMask36 , componentMask37 ) , 3.0 ) - 0.0) * (1.0 - 0.0) / (pow( _Range , 3.0 ) - 0.0)) , 0.0 , 1.0 );
			float temp_output_20_0 = ( 1.0 - clampResult16 );
			float4 lerpResult39 = lerp( min( _Color , triplanar41 ) , _EmissionColor , temp_output_20_0);
			float4 _Color0 = float4(0,0,0,0);
			float clampResult53 = clamp( ( 1.0 - (0.0 + (_SinTime.w - -1.0) * (1.0 - 0.0) / (ase_worldPos.y - -1.0)) ) , 0.0 , 1.0 );
			float4 lerpResult48 = lerp( ( lerpResult39 * triplanar41 ) , _Color0 , clampResult53);
			o.Albedo = lerpResult48.rgb;
			float4 lerpResult50 = lerp( ( triplanar41 * _EmissionColor * _EmissionPower ) , _Color0 , clampResult53);
			o.Emission = lerpResult50.rgb;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			# include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float3 worldPos : TEXCOORD6;
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				fixed3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			fixed4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				fixed3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=13101
7;29;1936;1116;1761.68;1464.739;1.922772;True;True
Node;AmplifyShaderEditor.Vector3Node;12;-1949.758,401.8673;Float;False;Property;_CharacterPosition;CharacterPosition;4;0;0,0,0;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.WorldPosInputsNode;14;-1891.911,131.9189;Float;False;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ComponentMaskNode;37;-1611.774,400.2594;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.ComponentMaskNode;36;-1597.314,127.0973;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.DistanceOpNode;13;-995.2973,252.4317;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;0.0,0,0;False;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;18;-969.5876,416.3288;Float;False;Property;_Range;Range;5;0;0;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.PowerNode;23;-757.4841,421.1487;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;3.0;False;1;FLOAT
Node;AmplifyShaderEditor.PowerNode;22;-754.2711,249.217;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;3.0;False;1;FLOAT
Node;AmplifyShaderEditor.TFHCRemap;15;-548.5963,250.8247;Float;False;5;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;20.0;False;3;FLOAT;0.0;False;4;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1559.297,-522.0634;Float;True;Property;_Texture;Texture;0;0;None;False;white;Auto;0;1;SAMPLER2D
Node;AmplifyShaderEditor.RangedFloatNode;43;-1215.124,-288.6766;Float;False;Property;_Tile;Tile;6;0;1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.SinTimeNode;54;-704.1556,-1160.942;Float;False;0;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.WorldPosInputsNode;46;-734.9193,-955.2057;Float;False;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.TriplanarNode;41;-917.6231,-512.4231;Float;True;Spherical;World;False;Top Texture 0;_TopTexture0;white;6;None;Mid Texture 0;_MidTexture0;white;7;None;Bot Texture 0;_BotTexture0;white;8;None;Triplanar Sampler;5;0;SAMPLER2D;;False;1;SAMPLER2D;;False;2;SAMPLER2D;;False;3;FLOAT;1.0;False;4;FLOAT;1.0;False;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ColorNode;9;-686.7847,-774.3365;Float;False;Property;_Color;Color;3;0;0,0,0,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ClampOpNode;16;-325.2466,250.8249;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.TFHCRemap;52;-290.7594,-889.8309;Float;False;5;0;FLOAT;0.0;False;1;FLOAT;-1.0;False;2;FLOAT;1.0;False;3;FLOAT;0.0;False;4;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMinNode;11;-232.0497,-628.1145;Float;False;2;0;COLOR;0.0;False;1;FLOAT4;0.0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.ColorNode;7;-905.3145,-144.4567;Float;False;Property;_EmissionColor;EmissionColor;1;0;0,0,0,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.OneMinusNode;20;-116.3576,249.218;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.OneMinusNode;55;-4.266403,-889.8306;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;8;-770.3407,72.4662;Float;False;Property;_EmissionPower;EmissionPower;2;0;0;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.LerpOp;39;158.9576,-530.0978;Float;False;3;0;FLOAT4;0.0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0.0;False;1;COLOR
Node;AmplifyShaderEditor.LerpOp;19;152.3773,157.6283;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;367.695,99.7816;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;-0.94;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-252.9399,-160.5251;Float;False;3;3;0;FLOAT4;0.0;False;1;COLOR;0.0,0,0,0;False;2;FLOAT;0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.ColorNode;44;-431.1221,-1191.707;Float;False;Constant;_Color0;Color 0;7;0;0,0,0,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;469.0766,-528.4917;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.ClampOpNode;53;295.6862,-887.9079;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.DynamicAppendNode;17;586.223,37.11556;Float;False;FLOAT4;4;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;3;FLOAT;0.0;False;1;FLOAT4
Node;AmplifyShaderEditor.NormalizeNode;28;-681.9634,748.9435;Float;False;1;0;FLOAT2;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.ClampOpNode;35;-186.5116,496.6696;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;2.0;False;1;FLOAT
Node;AmplifyShaderEditor.LerpOp;50;459.1225,-234.1656;Float;False;3;0;FLOAT4;0,0,0,0;False;1;COLOR;0.0,0,0,0;False;2;FLOAT;0.0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleSubtractOpNode;26;-953.5186,646.1049;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.LerpOp;48;687.9313,-859.0674;Float;False;3;0;COLOR;0.0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0.0;False;1;COLOR
Node;AmplifyShaderEditor.ComponentMaskNode;38;-1545.895,748.9427;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.SimpleSubtractOpNode;25;-948.6985,747.3362;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.DotProductOpNode;27;-387.9127,644.4991;Float;False;2;0;FLOAT2;0,0,0;False;1;FLOAT2;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.NormalizeNode;30;-680.3557,646.106;Float;False;1;0;FLOAT2;0,0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.WorldSpaceCameraPos;24;-1916.011,755.3705;Float;False;0;1;FLOAT3
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1206.251,-443.6348;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;CharacterBuildingShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;Opaque;0.5;True;True;0;False;Opaque;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0.0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;OBJECT;0.0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;37;0;12;0
WireConnection;36;0;14;0
WireConnection;13;0;36;0
WireConnection;13;1;37;0
WireConnection;23;0;18;0
WireConnection;22;0;13;0
WireConnection;15;0;22;0
WireConnection;15;2;23;0
WireConnection;41;0;1;0
WireConnection;41;3;43;0
WireConnection;16;0;15;0
WireConnection;52;0;54;4
WireConnection;52;2;46;2
WireConnection;11;0;9;0
WireConnection;11;1;41;0
WireConnection;20;0;16;0
WireConnection;55;0;52;0
WireConnection;39;0;11;0
WireConnection;39;1;7;0
WireConnection;39;2;20;0
WireConnection;19;1;14;2
WireConnection;19;2;20;0
WireConnection;33;0;19;0
WireConnection;6;0;41;0
WireConnection;6;1;7;0
WireConnection;6;2;8;0
WireConnection;40;0;39;0
WireConnection;40;1;41;0
WireConnection;53;0;55;0
WireConnection;17;1;33;0
WireConnection;28;0;25;0
WireConnection;35;0;27;0
WireConnection;50;0;6;0
WireConnection;50;1;44;0
WireConnection;50;2;53;0
WireConnection;26;0;36;0
WireConnection;26;1;37;0
WireConnection;48;0;40;0
WireConnection;48;1;44;0
WireConnection;48;2;53;0
WireConnection;38;0;24;0
WireConnection;25;0;38;0
WireConnection;25;1;37;0
WireConnection;27;0;30;0
WireConnection;27;1;28;0
WireConnection;30;0;26;0
WireConnection;0;0;48;0
WireConnection;0;2;50;0
WireConnection;0;11;17;0
ASEEND*/
//CHKSM=3BDA7CB2BB3CA9713D327AA87DAC040012721EA4