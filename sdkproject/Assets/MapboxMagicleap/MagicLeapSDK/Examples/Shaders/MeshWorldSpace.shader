// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2017 Magic Leap, Inc. (COMPANY) All Rights Reserved.
// Magic Leap, Inc. Confidential and Proprietary
//
//  NOTICE:  All information contained herein is, and remains the property
//  of COMPANY. The intellectual and technical concepts contained herein
//  are proprietary to COMPANY and may be covered by U.S. and Foreign
//  Patents, patents in process, and are protected by trade secret or
//  copyright law.  Dissemination of this information or reproduction of
//  this material is strictly forbidden unless prior written permission is
//  obtained from COMPANY.  Access to the source code contained herein is
//  hereby forbidden to anyone except current COMPANY employees, managers
//  or contractors who have executed Confidentiality and Non-disclosure
//  agreements explicitly covering such access.
//
//  The copyright notice above does not evidence any actual or intended
//  publication or disclosure  of  this source code, which includes
//  information that is confidential and/or proprietary, and is a trade
//  secret, of  COMPANY.   ANY REPRODUCTION, MODIFICATION, DISTRIBUTION,
//  PUBLIC  PERFORMANCE, OR PUBLIC DISPLAY OF OR THROUGH USE  OF THIS
//  SOURCE CODE  WITHOUT THE EXPRESS WRITTEN CONSENT OF COMPANY IS
//  STRICTLY PROHIBITED, AND IN VIOLATION OF APPLICABLE LAWS AND
//  INTERNATIONAL TREATIES.  THE RECEIPT OR POSSESSION OF  THIS SOURCE
//  CODE AND/OR RELATED INFORMATION DOES NOT CONVEY OR IMPLY ANY RIGHTS
//  TO REPRODUCE, DISCLOSE OR DISTRIBUTE ITS CONTENTS, OR TO MANUFACTURE,
//  USE, OR SELL ANYTHING THAT IT  MAY DESCRIBE, IN WHOLE OR IN PART.
//
// %COPYRIGHT_END%
// --------------------------------------------------------------------*/
// %BANNER_END%

// This shader determines which axis the normal of the surface is closest to.
// The axis will determine which world coordinates (xy, zy, or xz) would be used.
Shader "Magic Leap/Mesh World Space"
{
    Properties
    {
        _MainTex("Color (RGB) Alpha (A)", 2D) = "white" {}
        _Color("Color", Color) = (1,0,0,1)
    }

    SubShader
    {
        Tags{ "RenderType" = "Opaque" }

        Pass
        {
            ColorMask 0
        }

        LOD 200
        Cull Back

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf NoLighting noambient alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        //#pragma target 3.0

        sampler2D _MainTex;
        float4 _MainTex_ST;
        float4 _Color;

        struct Input
        {
            float3 worldNormal;
            float3 worldPos;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
            fixed4 c;
            c.rgb = s.Albedo;
            c.a = s.Alpha;
            return c;
        }

        // determine which axis, perpendicular to the normal, the normal is closest to
        void surf (Input IN, inout SurfaceOutput o)
        {
            float3 normal = IN.worldNormal;
            float3 pos = IN.worldPos;
            const float cos45 = 0.70710678118;
            half2 uvCoords = half2(0, 0);

            if (abs(normal.y) > cos45)
            {
                // normal is closer to global up or down
                uvCoords = pos.xz;
            }
            else if (abs(normal.x) > abs(normal.z))
            {
                // normal is closer to global right or left
                uvCoords = pos.zy;
            }
            else
            {
                // normal is closer to global forward or backward
                uvCoords = pos.xy;
            }

            float4 c = tex2D(_MainTex, uvCoords * _MainTex_ST.xy + _MainTex_ST.zw) * _Color;

            o.Emission = c.rgb;
            o.Alpha = c.a;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
