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
// This uses simple alpha blending and color tinting
Shader "Magic Leap/World XZ with Color"
{
    Properties
    {
        _BackgroundTex ("Background Texture", 2D) = "black" {}
        _BackgroundColor ("Background Color", Color) = (1,1,1,1)
        _ForegroundTex ("Foreground Texture", 2D) = "black" {}
        _ForegroundColor ("Foreground Color", Color) = (0,0,0,0)
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

        sampler2D _BackgroundTex;
        float4 _BackgroundTex_ST;
        float4 _BackgroundColor;

        sampler2D _ForegroundTex;
        float4 _ForegroundTex_ST;
        float4 _ForegroundColor;

        struct Input
        {
            float3 worldPos;
        };

        // Using the world coordinates and texture tiling + offset as UV for the texture
        // Using simple transparency
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 pos = IN.worldPos;
            float4 background = tex2D(_BackgroundTex, pos.xz * _BackgroundTex_ST.xy + _BackgroundTex_ST.zw) * _BackgroundColor;
            float4 foreground = tex2D(_ForegroundTex, pos.xz * _ForegroundTex_ST.xy + _ForegroundTex_ST.zw) * _ForegroundColor;
            o.Albedo = (1.0f - foreground.a) * background.rgb + foreground.a * foreground.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
