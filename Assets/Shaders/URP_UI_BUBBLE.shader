Shader "UI/URP_UI_Bubble"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Bubble Settings)]
        _BubbleColor ("Bubble Base Color", Color) = (1, 1, 1, 0.15)
        _EdgeGlow ("Edge Glow Intensity", Range(0, 4)) = 1.5
        
        [Header(Iridescence Rainbow Effect)]
        _ShiftSpeed ("Color Shift Speed", Range(0, 10)) = 2.0
        _ShiftFrequency ("Color Shift Density", Range(0, 10)) = 3.0
        _ShiftIntensity ("Rainbow Strength", Range(0, 1)) = 0.6
        
        // Required for UI Canvas Masking
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 positionUI   : TEXCOORD1;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            
            half4 _Color;
            half4 _BubbleColor;
            float _EdgeGlow;
            float _ShiftSpeed;
            float _ShiftFrequency;
            float _ShiftIntensity;

            // Function to generate rainbow bands based on a value
            float3 GetRainbow(float t)
            {
                float3 r = float3(t, t, t);
                r.r = sin(6.28318 * (t + 0.0)) * 0.5 + 0.5;
                r.g = sin(6.28318 * (t + 0.33)) * 0.5 + 0.5;
                r.b = sin(6.28318 * (t + 0.67)) * 0.5 + 0.5;
                return r;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                ZERO_INITIALIZE(Varyings, output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionUI = input.positionOS;
                output.uv = input.uv;
                output.color = input.color * _Color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample the base texture properly for modern HLSL
                half4 texColor = _MainTex.Sample(sampler_MainTex, input.uv);
                
                // Calculate distance from center for a procedural round bubble glow
                float2 centerUV = input.uv - float2(0.5, 0.5);
                float dist = length(centerUV) * 2.0;
                
                // Procedural bubble edge shimmer
                float edgeGlow = pow(saturate(dist), 3.0) * _EdgeGlow;
                
                // Create shifting rainbow iridescence using UV coordinates and time
                float timeFactor = _Time.y * _ShiftSpeed;
                float wave = (centerUV.x + centerUV.y) * _ShiftFrequency + timeFactor;
                float3 rainbow = GetRainbow(wave * 0.1);
                
                // Combine colors together
                float3 finalRGB = _BubbleColor.rgb;
                finalRGB += rainbow * _ShiftIntensity;
                finalRGB += edgeGlow * _BubbleColor.rgb;
                
                // Apply UI Element Tints and Masking modifiers
                half4 color = half4(finalRGB, _BubbleColor.a + (edgeGlow * 0.2));
                color *= texColor * input.color;

                return color;
            }
            ENDHLSL
        }
    }
}