// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TerrainShader"
{
	Properties
	{
		_MainTexture("MainTexture", 2D) = "white" {}
		_HeightTexture("HeightTexture", 2D) = "white" {}
		_TileScale("TileScale", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _MainTexture;
		uniform float4 _MainTexture_ST;
		uniform sampler2D _HeightTexture;
		uniform float4 _HeightTexture_ST;
		uniform float _TileScale;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 uv_HeightTexture = v.texcoord * _HeightTexture_ST.xy + _HeightTexture_ST.zw;
			float4 tex2DNode5 = tex2Dlod( _HeightTexture, float4( uv_HeightTexture, 0, 0) );
			float4 appendResult13 = (float4(0 , ( ( -10000.0 + ( ( ( ( tex2DNode5.r * 65536 ) * 255 ) + ( tex2DNode5.g * 65280 ) + ( tex2DNode5.b * 255 ) ) * 0.1 ) ) * _TileScale ) , 0 , 0));
			v.vertex.xyz += appendResult13.xyz;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_MainTexture = i.uv_texcoord * _MainTexture_ST.xy + _MainTexture_ST.zw;
			o.Albedo = tex2D( _MainTexture, uv_MainTexture ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=14501
-1864.8;470.4;1677;979;1308.368;224.7459;1.597484;True;True
Node;AmplifyShaderEditor.TexturePropertyNode;4;-1138.736,222.0157;Float;True;Property;_HeightTexture;HeightTexture;1;0;Create;True;0;None;None;False;white;Auto;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;5;-867.034,222.0157;Float;True;Property;_TextureSample1;Texture Sample 1;1;0;Create;True;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-511.838,258.0041;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;65536;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-507.9377,364.6035;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;65280;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-519.2098,516.7007;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;255;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-327.5118,277.0784;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;255;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-167.7125,340.9635;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-334.0583,196.0594;Float;False;Constant;_Float0;Float 0;2;0;Create;True;0;-10000;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-27.82992,340.9633;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;7;115.0154,202.5595;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;17;65.46918,502.3239;Float;False;Property;_TileScale;TileScale;2;0;Create;True;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;1;-220.2097,-36.58354;Float;True;Property;_MainTexture;MainTexture;0;0;Create;True;0;None;None;False;white;Auto;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;276.3371,205.192;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;60.42059,-37.35408;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;13;439.9681,182.0565;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;678.4723,-31.0029;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;TerrainShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;0;0;0;0;False;2;15;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;OFF;OFF;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;False;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;0;4;0
WireConnection;8;0;5;1
WireConnection;10;0;5;2
WireConnection;15;0;5;3
WireConnection;14;0;8;0
WireConnection;9;0;14;0
WireConnection;9;1;10;0
WireConnection;9;2;15;0
WireConnection;12;0;9;0
WireConnection;7;0;6;0
WireConnection;7;1;12;0
WireConnection;16;0;7;0
WireConnection;16;1;17;0
WireConnection;3;0;1;0
WireConnection;13;1;16;0
WireConnection;0;0;3;0
WireConnection;0;11;13;0
ASEEND*/
//CHKSM=88FE0EB526C715218F31202C3B76EAD0ABA3EE67