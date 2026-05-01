// MopClean.shader
// Pure HLSL blit shader — NO ShaderGraph, NO dependencies.
// Used by MopCleaner.cs via Graphics.Blit every frame.
//
// Logic per pixel:
//   dist     = length(pixelUV - _HitUV)
//   falloff  = 1 - smoothstep(_Radius * 0.5, _Radius, dist)   <- soft circle
//   erasure  = falloff * _Strength                             <- tiny amount
//   output.r = clamp(previousFrame.r - erasure, 0, 1)         <- gradual clean
//
// Only the R channel is written. FloorBloodShader reads R as blood coverage.

Shader "Custom/MopClean"
{
    Properties
    {
        // All four are set by MopCleaner.cs at runtime — don't touch in Inspector
        _MainTex  ("Previous Frame (auto)",  2D)           = "white" {}
        _HitUV    ("Mop UV Hit Point",       Vector)       = (0.5, 0.5, 0, 0)
        _Radius   ("Brush Radius (UV)",      Float)        = 0.05
        _Strength ("Erase Amount per Frame", Float)        = 0.04
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // Standard blit pass settings
        Cull   Off
        ZWrite Off
        ZTest  Always

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // ── Uniforms ────────────────────────────────────────────────────
            sampler2D _MainTex;   // previous frame, set by Graphics.Blit source
            float4    _HitUV;     // xy = mop contact UV
            float     _Radius;    // brush outer radius in UV space
            float     _Strength;  // amount cleaned per blit (e.g. 0.04)

            // ── Vertex ──────────────────────────────────────────────────────
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // ── Fragment ────────────────────────────────────────────────────
            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Read current blood coverage at this pixel (0=clean, 1=blood)
                fixed4 prev = tex2D(_MainTex, i.uv);

                // 2. Distance from this pixel to the mop contact point
                float dist = length(i.uv - _HitUV.xy);

                // 3. Soft circular falloff  (1 = dead centre, 0 = outside brush)
                //    Inner half is full-strength; outer half fades smoothly
                float falloff = 1.0 - smoothstep(_Radius * 0.5, _Radius, dist);

                // 4. How much to erase this frame
                float erasure = falloff * _Strength;

                // 5. Subtract from current coverage, never go below 0
                float cleaned = saturate(prev.r - erasure);

                // 6. Write back — keep G/B untouched in case FloorBloodShader uses them
                return fixed4(cleaned, prev.g, prev.b, cleaned);
            }
            ENDCG
        }
    }
}
