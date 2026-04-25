Shader "Custom/DrawShader"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _Coordinate ("Coordinate", Vector) = (0,0,0,0)
        _Size ("Size", Float) = 0.05
        _Strength ("Strength", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _Coordinate;
            float _Size;
            float _Strength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dist = distance(i.uv, _Coordinate.xy);

                // base circle
                float circle = smoothstep(_Size, _Size * 0.5, dist);

                // 🔥 add noise effect (break the circle)
                float noise = frac(sin(dot(i.uv * 100, float2(12.9898,78.233))) * 43758.5453);

                float splatter = (1 - circle) * step(0.3, noise);

                float4 col = tex2D(_MainTex, i.uv);

                float paint = splatter * _Strength * 0.2;
                col.r = saturate(col.r + paint);

                return col;
            }
            ENDCG
        }
    }
}