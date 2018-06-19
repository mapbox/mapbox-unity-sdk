// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CharacterBuildingShader"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
		_Texture("Texture", 2D) = "white" {}
		_CharacterPosition("CharacterPosition", Vector) = (0,0,0,0)
		_Range("Range", Float) = 0
		_Tile("Tile", Float) = 1
		_FadeEnd("FadeEnd", Float) = 2
		_Color("Color", Color) = (0,0,0,0)
		_BaseColor("BaseColor", Color) = (0,0,0,0)
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

		uniform float4 _BaseColor;
		uniform sampler2D _Texture;
		uniform float _Tile;
		uniform float4 _Color;
		uniform float _FadeEnd;
		uniform float3 _CharacterPosition;
		uniform float _Range;


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


		float3 mod289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex);
			float2 componentMask36 = ase_worldPos.xz;
			float2 componentMask37 = _CharacterPosition.xz;
			float clampResult16 = clamp( (0.0 + (pow( distance( componentMask36 , componentMask37 ) , 3.0 ) - 0.0) * (1.0 - 0.0) / (pow( _Range , 3.0 ) - 0.0)) , 0.0 , 1.0 );
			float lerpResult19 = lerp( 0.0 , ase_worldPos.y , ( 1.0 - clampResult16 ));
			float4 appendResult17 = (float4(0.0 , ( lerpResult19 * -0.94 ) , 0.0 , 0.0));
			v.vertex.xyz += appendResult17.xyz;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float4 triplanar41 = TriplanarSampling( _Texture, _Texture, _Texture, ase_worldPos, ase_worldNormal, 1.0, _Tile, 0 );
			float2 componentMask69 = ase_worldPos.xz;
			float mulTime75 = _Time.y * 0.2;
			float simplePerlin2D68 = snoise( ( ( componentMask69 / float2( 8,8 ) ) + mulTime75 ) );
			float clampResult66 = clamp( (0.0 + (( simplePerlin2D68 + ase_worldPos.y ) - 0.0) * (1.0 - 0.0) / (_FadeEnd - 0.0)) , 0.0 , 1.0 );
			float clampResult53 = clamp( ( 1.0 - clampResult66 ) , 0.1 , 1.0 );
			float4 lerpResult48 = lerp( _BaseColor , ( triplanar41 * _Color ) , clampResult53);
			o.Albedo = lerpResult48.xyz;
			o.Emission = lerpResult48.xyz;
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
7;29;1936;1116;1388.344;1201.545;1.713429;True;True
Node;AmplifyShaderEditor.WorldPosInputsNode;46;-1380.454,-379.1331;Float;False;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ComponentMaskNode;69;-1094.655,-494.2093;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.Vector3Node;12;-1637.411,401.8673;Float;False;Property;_CharacterPosition;CharacterPosition;1;0;0,0,0;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.WorldPosInputsNode;14;-1579.564,131.9189;Float;False;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SimpleDivideOpNode;73;-818.0357,-490.5296;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;8,8;False;1;FLOAT2
Node;AmplifyShaderEditor.SimpleTimeNode;75;-860.8231,-379.5167;Float;False;1;0;FLOAT;0.2;False;1;FLOAT
Node;AmplifyShaderEditor.ComponentMaskNode;37;-1299.427,400.2594;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.ComponentMaskNode;36;-1284.967,127.0973;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.SimpleAddOpNode;74;-617.1793,-401.9212;Float;False;2;2;0;FLOAT2;0.0;False;1;FLOAT;0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.RangedFloatNode;18;-969.5876,416.3288;Float;False;Property;_Range;Range;2;0;0;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.DistanceOpNode;13;-995.2973,252.4317;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;0.0,0,0;False;1;FLOAT
Node;AmplifyShaderEditor.NoiseGeneratorNode;68;-429.8528,-381.6138;Float;False;Simplex2D;1;0;FLOAT2;100,100;False;1;FLOAT
Node;AmplifyShaderEditor.PowerNode;22;-754.2711,249.217;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;3.0;False;1;FLOAT
Node;AmplifyShaderEditor.PowerNode;23;-757.4841,421.1487;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;3.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleAddOpNode;72;-159.4099,-353.3119;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.TFHCRemap;15;-548.5963,250.8247;Float;False;5;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;20.0;False;3;FLOAT;0.0;False;4;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;56;-181.0639,-235.8129;Float;False;Property;_FadeEnd;FadeEnd;4;0;2;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.ClampOpNode;16;-325.2466,250.8249;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.TFHCRemap;52;111.2249,-362.403;Float;False;5;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;3;FLOAT;0.0;False;4;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.ClampOpNode;66;344.3752,-360.1194;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.TexturePropertyNode;1;-517.5781,-948.9332;Float;True;Property;_Texture;Texture;0;0;None;False;white;Auto;0;1;SAMPLER2D
Node;AmplifyShaderEditor.RangedFloatNode;43;-310.076,-726.5656;Float;False;Property;_Tile;Tile;3;0;1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.OneMinusNode;20;-116.3576,249.218;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.OneMinusNode;55;539.5688,-364.1947;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.ColorNode;60;59.77039,-631.2576;Float;False;Property;_Color;Color;5;0;0,0,0,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.LerpOp;19;256.4929,157.6283;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.TriplanarNode;41;1139.025,-323.3693;Float;True;Spherical;World;False;Top Texture 0;_TopTexture0;white;6;None;Mid Texture 0;_MidTexture0;white;7;None;Bot Texture 0;_BotTexture0;white;8;None;Triplanar Sampler;5;0;SAMPLER2D;;False;1;SAMPLER2D;;False;2;SAMPLER2D;;False;3;FLOAT;1.0;False;4;FLOAT;1.0;False;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;515.9266,-657.8391;Float;False;2;2;0;FLOAT4;0.0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.ClampOpNode;53;734.0475,-367.4904;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.1;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;471.8105,99.7816;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;-0.94;False;1;FLOAT
Node;AmplifyShaderEditor.ColorNode;67;503.5923,-900.1298;Float;False;Property;_BaseColor;BaseColor;6;0;0,0,0,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.LerpOp;48;841.2689,-681.2978;Float;False;3;0;COLOR;0.0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0.0;False;1;FLOAT4
Node;AmplifyShaderEditor.DynamicAppendNode;17;753.7844,56.63725;Float;False;FLOAT4;4;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;3;FLOAT;0.0;False;1;FLOAT4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1364.302,-286.3154;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;CharacterBuildingShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;Opaque;0.5;True;True;0;False;Opaque;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0.0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;OBJECT;0.0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;69;0;46;0
WireConnection;73;0;69;0
WireConnection;37;0;12;0
WireConnection;36;0;14;0
WireConnection;74;0;73;0
WireConnection;74;1;75;0
WireConnection;13;0;36;0
WireConnection;13;1;37;0
WireConnection;68;0;74;0
WireConnection;22;0;13;0
WireConnection;23;0;18;0
WireConnection;72;0;68;0
WireConnection;72;1;46;2
WireConnection;15;0;22;0
WireConnection;15;2;23;0
WireConnection;16;0;15;0
WireConnection;52;0;72;0
WireConnection;52;2;56;0
WireConnection;66;0;52;0
WireConnection;20;0;16;0
WireConnection;55;0;66;0
WireConnection;19;1;14;2
WireConnection;19;2;20;0
WireConnection;41;0;1;0
WireConnection;41;3;43;0
WireConnection;61;0;41;0
WireConnection;61;1;60;0
WireConnection;53;0;55;0
WireConnection;33;0;19;0
WireConnection;48;0;67;0
WireConnection;48;1;61;0
WireConnection;48;2;53;0
WireConnection;17;1;33;0
WireConnection;0;0;48;0
WireConnection;0;2;48;0
WireConnection;0;11;17;0
ASEEND*/
//CHKSM=A284E886CB8D7C3EA8E4575607D42C64D3005429