// MIT License
//
// Copyright (c) 2021 Akihiro Noguchi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

Shader "ImageEffect/DualFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _DstTexelSize;
        half _Opacity;

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct downV2f
        {
            float4 vertex : SV_POSITION;
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float2 uv2 : TEXCOORD2;
            float2 uv3 : TEXCOORD3;
            float2 uv4 : TEXCOORD4;
        };

        downV2f downVert(appdata v)
        {
            const float2 halfTexel = _DstTexelSize.xy * 0.5;
            downV2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv0 = v.uv;
            o.uv1 = v.uv + float2(-halfTexel.x, -halfTexel.y);
            o.uv2 = v.uv + float2(halfTexel.x, -halfTexel.y);
            o.uv3 = v.uv + float2(-halfTexel.x, halfTexel.y);
            o.uv4 = v.uv + float2(halfTexel.x, halfTexel.y);
            return o;
        }

        half4 downFrag(downV2f i) : SV_Target
        {
            return tex2D(_MainTex, i.uv0) * 0.5h +
                tex2D(_MainTex, i.uv1) * (1.0h / 8.0h) +
                tex2D(_MainTex, i.uv2) * (1.0h / 8.0h) +
                tex2D(_MainTex, i.uv3) * (1.0h / 8.0h) +
                tex2D(_MainTex, i.uv4) * (1.0h / 8.0h);
        }

        struct upV2f
        {
            float4 vertex : SV_POSITION;
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float2 uv2 : TEXCOORD2;
            float2 uv3 : TEXCOORD3;
            float2 uv4 : TEXCOORD4;
            float2 uv5 : TEXCOORD5;
            float2 uv6 : TEXCOORD6;
            float2 uv7 : TEXCOORD7;
        };

        upV2f upVert(appdata v)
        {
            const float2 halfTexel = _DstTexelSize.xy * 0.5;
            upV2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv0 = v.uv + float2(-halfTexel.x, -halfTexel.y) * 2.0f;
            o.uv1 = v.uv + float2(halfTexel.x, -halfTexel.y) * 2.0f;
            o.uv2 = v.uv + float2(-halfTexel.x, halfTexel.y) * 2.0f;
            o.uv3 = v.uv + float2(halfTexel.x, halfTexel.y) * 2.0f;
            o.uv4 = v.uv + float2(0, -halfTexel.y) * 4.0f;
            o.uv5 = v.uv + float2(-halfTexel.x, 0) * 4.0f;
            o.uv6 = v.uv + float2(halfTexel.x, 0) * 4.0f;
            o.uv7 = v.uv + float2(0, halfTexel.y) * 4.0f;
            return o;
        }

        half4 upSample(upV2f i)
        {
            return tex2D(_MainTex, i.uv0) * (1.0h / 6.0h) +
                tex2D(_MainTex, i.uv1) * (1.0h / 6.0h) +
                tex2D(_MainTex, i.uv2) * (1.0h / 6.0h) +
                tex2D(_MainTex, i.uv3) * (1.0h / 6.0h) +
                tex2D(_MainTex, i.uv4) * (1.0h / 12.0h) +
                tex2D(_MainTex, i.uv5) * (1.0h / 12.0h) +
                tex2D(_MainTex, i.uv6) * (1.0h / 12.0h) +
                tex2D(_MainTex, i.uv7) * (1.0h / 12.0h);
        }

        half4 upFrag(upV2f i) : SV_Target
        {
            return upSample(i);
        }

        half4 upFragOpacity(upV2f i) : SV_Target
        {
            half4 res = upSample(i);
            res.a = _Opacity;
            return res;
        }
        ENDHLSL

        Pass
        {
            Name "Downsample"

            HLSLPROGRAM
            #pragma vertex downVert
            #pragma fragment downFrag
            ENDHLSL
        }

        Pass
        {
            Name "Upsample"

            HLSLPROGRAM
            #pragma vertex upVert
            #pragma fragment upFrag
            ENDHLSL
        }

        Pass
        {
            Name "Upsample with Opacity"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex upVert
            #pragma fragment upFragOpacity
            ENDHLSL
        }
    }
}