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

// This shader uses the world position (XZ) of the pixel instead of the model's UV.
// This uses lerp blending, ignoring the alpha
Shader "Magic Leap/World XZ with Lerp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _BlendTex ("Blend Texture", 2D) = "black" {}
        _Lerp ("Lerp Parameter", Range(0, 1)) = 0.5
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

        sampler2D _MainTex;
        float4 _MainTex_ST;
        sampler2D _BlendTex;
        float4 _BlendTex_ST;
        fixed _Lerp;

        struct Input
        {
            float3 worldPos;
        };

        // Using the world coordinates and texture tiling + offset as UV for the texture
        // Lerping through the 2 colors
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 pos = IN.worldPos;
            float3 base = tex2D(_MainTex, pos.xz * _MainTex_ST.xy + _MainTex_ST.zw).rgb;
            float3 blend = tex2D(_BlendTex, pos.xz * _BlendTex_ST.xy + _BlendTex_ST.zw).rgb;
            o.Albedo = (1 - _Lerp) * base + _Lerp * blend;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
