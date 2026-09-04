// The zone wall, with an optional frosting of whatever is behind it.
//
// Loaded through uMod's asset API rather than as a Material, because a Material would be a GUID reference and
// this repo does not track .meta files: a fresh clone would get a broken link.
//
// Deliberately keyword-free. Shader features get variant-stripped out of a umod, and a blur that silently
// compiles to nothing is worse than no blur at all.

Shader "ClosingCircle/Wall"
{
    Properties
    {
        _BaseMap ("Alpha ramp", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 0.235, 0.235, 0.24)
        _Blur ("Blur", Range(0, 1)) = 0
        _BlurRadius ("Blur radius", Range(0, 0.05)) = 0.018
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ZoneWall"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Blur;
                half _BlurRadius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.screenPos = positions.positionNDC;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                return OUT;
            }

            #define TAP_COUNT 12

            // Half a metre of slack, so a surface at the same depth as the wall is not mistaken for one in
            // front of it and rejected along a silhouette.
            #define DEPTH_SLACK 0.5

            half3 Frost (float2 screenUV, float wallEye, half amount)
            {
                half3 centre = SampleSceneColor(screenUV);

                float spread = amount * _BlurRadius;
                if (spread <= 0.0) return centre;

                // Rotated per pixel. A fixed ring puts every tap in the same handful of directions on every
                // pixel, which the eye reads as ghost copies of an object rather than as blur; scattering the
                // offsets turns that into fine noise, which is what frosted glass actually looks like.
                float noise = frac(sin(dot(screenUV, float2(12.9898, 78.233))) * 43758.5453);
                float rotation = noise * 6.2831853;

                // UV space is not square, so an unadjusted offset blurs further horizontally than vertically.
                float aspect = _ScreenParams.y / max(_ScreenParams.x, 1.0);

                half3 sum = 0;
                half weight = 0;

                [unroll]
                for (int i = 0; i < TAP_COUNT; i++)
                {
                    // A Vogel disc: even coverage over the whole area, without the spokes a ring leaves.
                    float angle = i * 2.39996323 + rotation;
                    float radius = sqrt((i + 0.5) / TAP_COUNT);

                    float2 offset = float2(cos(angle) * aspect, sin(angle)) * radius * spread;
                    float2 uv = clamp(screenUV + offset, 0.001, 0.999);

                    float eye = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);

                    // A tap nearer than the wall belongs to something between the camera and the boundary.
                    // Smearing that across the wall is what made trees inside the circle look doubled.
                    half3 colour = eye < wallEye - DEPTH_SLACK ? centre : SampleSceneColor(uv);

                    // Falling off toward the rim is the difference between a gaussian and a box average.
                    half w = exp(-2.0 * radius * radius);

                    sum += colour * w;
                    weight += w;
                }

                return sum / max(weight, 0.0001);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float wallEye = IN.screenPos.w;

                half ramp = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a;

                // Flat: exactly the wall the mod drew before the shader existed, so blur 0 changes nothing.
                half3 flatRGB = _BaseColor.rgb;
                half flatA = _BaseColor.a;

                // Frosted: the blurred scene tinted by the wall colour rather than multiplied by it. A
                // multiply zeroes whatever channels the tint is low in, which drained the colour out of
                // everything seen through a red wall.
                half3 frosted = Frost(screenUV, wallEye, _Blur);
                half3 frostRGB = lerp(frosted, _BaseColor.rgb, _BaseColor.a);

                // Frost has to cover to be seen at all: the frame already holds the sharp scene, so a low
                // alpha would just blend the blur back into the original.
                half3 rgb = lerp(flatRGB, frostRGB, _Blur);
                half alpha = ramp * lerp(flatA, 1.0, _Blur);

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
