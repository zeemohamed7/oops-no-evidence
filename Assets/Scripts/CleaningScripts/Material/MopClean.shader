// MopClean.shader
// Pure HLSL blit shader — NO ShaderGraph, NO dependencies.
// Used by MopCleaner.cs via Graphics.Blit every frame.
//
// Clean mode  (_Spread = 0):
//   output.r = clamp(prev.r - falloff * _Strength, 0, 1)   <- removes blood
//
// Spread mode (_Spread = 1):
//   output.r = clamp(prev.r + falloff * _Strength, 0, 1)   <- adds blood back
//
Shader "Custom/MopClean"
{
    Properties
    {
        // All set by MopCleaner.cs at runtime — don't touch in Inspector
        _MainTex  ("Previous Frame (auto)",  2D)     = "white" {}
        _HitUV    ("Mop UV Hit Point",       Vector)  = (0.5, 0.5, 0, 0)
        _Radius   ("Brush Radius (UV)",      Float)   = 0.05
        _Strength ("Erase/Spread Amount",    Float)   = 0.04
        _Spread   ("Spread Mode (0=clean 1=spread)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
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
            sampler2D _MainTex;
            float4    _HitUV;
            float     _Radius;
            float     _Strength;
            float     _Spread;    // 0 = erase blood, 1 = spread blood

            // ── Vertex ──────────────────────────────────────────────────────
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

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
                // 1. Read current blood coverage (0 = clean, 1 = blood)
                fixed4 prev = tex2D(_MainTex, i.uv);

                // 2. Distance from pixel to mop contact point
                float dist = length(i.uv - _HitUV.xy);

                // 3. Soft circular falloff (1 = centre, 0 = outside brush)
                float falloff = 1.0 - smoothstep(_Radius * 0.75, _Radius, dist);

                // 4. Delta: positive when spreading, negative when cleaning
                //    _Spread = 0 → subtract (clean)
                //    _Spread = 1 → add      (spread)
                float delta = falloff * _Strength * lerp(-1.0, 1.0, _Spread);

                // 5. Apply and clamp to [0, 1]
                float result = saturate(prev.r + delta);

                // 6. Write back — keep G/B untouched
                return fixed4(result, prev.g, prev.b, result);
            }
            ENDCG
        }
    }
}
