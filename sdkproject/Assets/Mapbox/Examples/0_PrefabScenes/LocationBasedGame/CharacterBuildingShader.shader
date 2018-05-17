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
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
		};

		uniform float4 _Color;
		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform float4 _EmissionColor;
		uniform float3 _CharacterPosition;
		uniform float _Range;
		uniform float _EmissionPower;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul(unity_ObjectToWorld, v.vertex);
			float2 componentMask36 = ase_worldPos.xz;
			float2 componentMask37 = _CharacterPosition.xz;
			float clampResult16 = clamp( (0.0 + (pow( distance( componentMask36 , componentMask37 ) , 3.0 ) - 0.0) * (1.0 - 0.0) / (pow( _Range , 3.0 ) - 0.0)) , 0.0 , 1.0 );
			float temp_output_20_0 = ( 1.0 - clampResult16 );
			float lerpResult19 = lerp( 0.0 , ase_worldPos.y , temp_output_20_0);
			float4 appendResult17 = (float4(0.0 , ( lerpResult19 * -0.96 ) , 0.0 , 0.0));
			v.vertex.xyz += appendResult17.xyz;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			float4 tex2DNode4 = tex2D( _Texture, uv_Texture );
			float3 ase_worldPos = i.worldPos;
			float2 componentMask36 = ase_worldPos.xz;
			float2 componentMask37 = _CharacterPosition.xz;
			float clampResult16 = clamp( (0.0 + (pow( distance( componentMask36 , componentMask37 ) , 3.0 ) - 0.0) * (1.0 - 0.0) / (pow( _Range , 3.0 ) - 0.0)) , 0.0 , 1.0 );
			float temp_output_20_0 = ( 1.0 - clampResult16 );
			float4 lerpResult39 = lerp( min( _Color , tex2DNode4 ) , _EmissionColor , temp_output_20_0);
			o.Albedo = ( lerpResult39 * tex2DNode4 ).rgb;
			o.Emission = ( tex2DNode4 * _EmissionColor * _EmissionPower ).xyz;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=13101
7;29;1936;1116;1446.272;963.9442;1.606836;True;True
Node;AmplifyShaderEditor.Vector3Node;12;-1949.758,401.8673;Float;False;Property;_CharacterPosition;CharacterPosition;4;0;0,0,0;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.WorldPosInputsNode;14;-1891.911,131.9189;Float;False;0;4;FLOAT3;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ComponentMaskNode;37;-1611.774,400.2594;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.ComponentMaskNode;36;-1597.314,127.0973;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.DistanceOpNode;13;-995.2973,252.4317;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;0.0,0,0;False;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;18;-969.5876,416.3288;Float;False;Property;_Range;Range;5;0;0;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.PowerNode;23;-757.4841,421.1487;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;3.0;False;1;FLOAT
Node;AmplifyShaderEditor.PowerNode;22;-754.2711,249.217;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;3.0;False;1;FLOAT
Node;AmplifyShaderEditor.TFHCRemap;15;-548.5963,250.8247;Float;False;5;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;20.0;False;3;FLOAT;0.0;False;4;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.TextureCoordinatesNode;3;-1295.776,-252.1149;Float;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1292.563,-483.4992;Float;True;Property;_Texture;Texture;0;0;None;False;white;Auto;0;1;SAMPLER2D
Node;AmplifyShaderEditor.ClampOpNode;16;-325.2466,250.8249;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;1.0;False;1;FLOAT
Node;AmplifyShaderEditor.ColorNode;9;-686.7847,-774.3365;Float;False;Property;_Color;Color;3;0;0,0,0,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SamplerNode;4;-839.4348,-493.1403;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT4;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.OneMinusNode;20;-116.3576,249.218;Float;False;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMinNode;11;-342.9215,-562.2343;Float;False;2;0;COLOR;0.0;False;1;FLOAT4;0.0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.ColorNode;7;-905.3145,-144.4567;Float;False;Property;_EmissionColor;EmissionColor;1;0;0,0,0,0;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.LerpOp;19;152.3773,157.6283;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.LerpOp;39;112.3593,-560.6279;Float;False;3;0;FLOAT4;0.0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0.0;False;1;COLOR
Node;AmplifyShaderEditor.RangedFloatNode;8;-770.3407,72.4662;Float;False;Property;_EmissionPower;EmissionPower;2;0;0;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;367.695,99.7816;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;-0.96;False;1;FLOAT
Node;AmplifyShaderEditor.DotProductOpNode;27;-387.9127,644.4991;Float;False;2;0;FLOAT2;0,0,0;False;1;FLOAT2;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.ClampOpNode;35;-186.5116,496.6696;Float;False;3;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;2.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;385.5211,-432.0816;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.ComponentMaskNode;38;-1545.895,748.9427;Float;False;True;False;True;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.DynamicAppendNode;17;574.9753,-1.448489;Float;False;FLOAT4;4;0;FLOAT;0.0;False;1;FLOAT;0.0;False;2;FLOAT;0.0;False;3;FLOAT;0.0;False;1;FLOAT4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;26;-953.5186,646.1049;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-252.9399,-160.5251;Float;False;3;3;0;FLOAT4;0.0;False;1;COLOR;0.0,0,0,0;False;2;FLOAT;0,0,0,0;False;1;FLOAT4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;25;-948.6985,747.3362;Float;False;2;0;FLOAT2;0.0;False;1;FLOAT2;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.NormalizeNode;30;-680.3557,646.106;Float;False;1;0;FLOAT2;0,0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.NormalizeNode;28;-681.9634,748.9435;Float;False;1;0;FLOAT2;0,0,0;False;1;FLOAT2
Node;AmplifyShaderEditor.WorldSpaceCameraPos;24;-1916.011,755.3705;Float;False;0;1;FLOAT3
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;819.0347,-256.9186;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;CharacterBuildingShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;Opaque;0.5;True;True;0;False;Opaque;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0.0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;OBJECT;0.0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;37;0;12;0
WireConnection;36;0;14;0
WireConnection;13;0;36;0
WireConnection;13;1;37;0
WireConnection;23;0;18;0
WireConnection;22;0;13;0
WireConnection;15;0;22;0
WireConnection;15;2;23;0
WireConnection;16;0;15;0
WireConnection;4;0;1;0
WireConnection;4;1;3;0
WireConnection;20;0;16;0
WireConnection;11;0;9;0
WireConnection;11;1;4;0
WireConnection;19;1;14;2
WireConnection;19;2;20;0
WireConnection;39;0;11;0
WireConnection;39;1;7;0
WireConnection;39;2;20;0
WireConnection;33;0;19;0
WireConnection;27;0;30;0
WireConnection;27;1;28;0
WireConnection;35;0;27;0
WireConnection;40;0;39;0
WireConnection;40;1;4;0
WireConnection;38;0;24;0
WireConnection;17;1;33;0
WireConnection;26;0;36;0
WireConnection;26;1;37;0
WireConnection;6;0;4;0
WireConnection;6;1;7;0
WireConnection;6;2;8;0
WireConnection;25;0;38;0
WireConnection;25;1;37;0
WireConnection;30;0;26;0
WireConnection;28;0;25;0
WireConnection;0;0;40;0
WireConnection;0;2;6;0
WireConnection;0;11;17;0
ASEEND*/
//CHKSM=3EDCAF6E8024F3BFDD5BCE5F3FCE7C5F3648D725