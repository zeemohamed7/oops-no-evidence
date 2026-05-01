// Custom/Footprint.shader
// Transparent decal — only the shoe shape is visible, background is fully clear.
// Assign a footprint texture where:
//   White/bright pixels = shoe shape (will show as red)
//   Black/transparent pixels = invisible (floor shows through)

Shader "Custom/Footprint"
{
    Properties
    {
        _MainTex  ("Footprint Mask (white=shoe, black=transparent)", 2D) = "white" {}
        _Color    ("Blood Color", Color) = (0.55, 0.02, 0.02, 1.0)
        _Alpha    ("Overall Alpha", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+10"
            "IgnoreProjector" = "True"
        }

        ZWrite Off
        ZTest  LEqual
        Blend  SrcAlpha OneMinusSrcAlpha
        Cull   Off
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4    _Color;
            float     _Alpha;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Use the texture's RED channel as the shoe mask
                // White pixels in the texture = full shoe colour
                // Black pixels = fully transparent (floor shows through)
                float mask = tex.r;

                return fixed4(_Color.rgb, mask * _Alpha * _Color.a);
            }
            ENDCG
        }
    }
}
