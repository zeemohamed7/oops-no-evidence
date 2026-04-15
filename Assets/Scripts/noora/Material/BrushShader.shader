Shader "Custom/BrushShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Coordinate ("Coordinate", Vector) = (0,0,0,0)
        _Size ("Size", Float) = 0.1
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = abs(i.uv - _Coordinate.xy);

                float2 size = float2(_Size * 1.0, _Size * 0.4);     

                float2 d = p - size;
                float dist = max(d.x, d.y);

                float strength = saturate(1.0 - smoothstep(0.0, 0.02, dist));

                float4 current = tex2D(_MainTex, i.uv);

                current.rgb *= (1.0 - strength * 0.5);

                return current;
            }
            ENDCG
        }
    }
}