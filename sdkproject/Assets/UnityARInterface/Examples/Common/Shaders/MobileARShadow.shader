//This is based on a shader from https://alastaira.wordpress.com/2014/12/30/adding-shadows-to-a-unity-vertexfragment-shader-in-7-easy-steps/

Shader "Custom/MobileARShadow"
{
    SubShader {
        Pass {
         
            // 1.) This will be the base forward rendering pass in which ambient, vertex, and
            // main directional light will be applied. Additional lights will need additional passes
            // using the "ForwardAdd" lightmode.
            // see: http://docs.unity3d.com/Manual/SL-PassTags.html
            Tags { "LightMode" = "ForwardBase" "RenderType"="Opaque" "Queue"="Geometry+1" "ForceNoShadowCasting"="True"  }
			LOD 150
			Blend Zero SrcColor
			ZWrite On
        
            CGPROGRAM
 
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            // 2.) This matches the "forward base" of the LightMode tag to ensure the shader compiles
            // properly for the forward bass pass. As with the LightMode tag, for any additional lights
            // this would be changed from _fwdbase to _fwdadd.
            #pragma multi_compile_fwdbase
 
            // 3.) Reference the Unity library that includes all the lighting shadow macros
            #include "AutoLight.cginc"
 
 
            struct v2f
            {
                float4 pos : SV_POSITION;
                 
                // 4.) The LIGHTING_COORDS macro (defined in AutoLight.cginc) defines the parameters needed to sample 
                // the shadow map. The (0,1) specifies which unused TEXCOORD semantics to hold the sampled values - 
                // As I'm not using any texcoords in this shader, I can use TEXCOORD0 and TEXCOORD1 for the shadow 
                // sampling. If I was already using TEXCOORD for UV coordinates, say, I could specify
                // LIGHTING_COORDS(1,2) instead to use TEXCOORD1 and TEXCOORD2.
                LIGHTING_COORDS(0,1)
            };
 
 
            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos (v.vertex);
                 
                // 5.) The TRANSFER_VERTEX_TO_FRAGMENT macro populates the chosen LIGHTING_COORDS in the v2f structure
                // with appropriate values to sample from the shadow/lighting map
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                 
                return o;
            }
 
            fixed4 frag(v2f i) : COLOR {
             
                // 6.) The LIGHT_ATTENUATION samples the shadowmap (using the coordinates calculated by TRANSFER_VERTEX_TO_FRAGMENT
                // and stored in the structure defined by LIGHTING_COORDS), and returns the value as a float.
                float attenuation = LIGHT_ATTENUATION(i);
                return fixed4(1.0,1.0,1.0,1.0) * attenuation;
            }
 
            ENDCG
        }
    }
     
    // 7.) To receive or cast a shadow, shaders must implement the appropriate "Shadow Collector" or "Shadow Caster" pass.
    // Although we haven't explicitly done so in this shader, if these passes are missing they will be read from a fallback
    // shader instead, so specify one here to import the collector/caster passes used in that fallback.
    Fallback "VertexLit"

}