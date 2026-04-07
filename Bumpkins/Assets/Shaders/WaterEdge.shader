Shader "Custom/WaterEdge"
{
    Properties
    {
        _MainTex  ("Sprite",                    2D)         = "white" {}
        [IntRange]
        _Direction("Direction 0=L 1=R 2=T 3=B", Range(0,3)) = 0
        _NoiseSeed("Noise Seed",                Float)      = 1.0
        _NoiseFreq("Noise Frequency",           Float)      = 5.0
        _NoiseBand("Noise Band Width",          Float)      = 0.18
        _NoiseAmp ("Noise Amplitude",           Float)      = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float     _Direction;
            float     _NoiseSeed;
            float     _NoiseFreq;
            float     _NoiseBand;
            float     _NoiseAmp;

            // Deterministic hash for a 2D grid point
            float hash(float2 p)
            {
                p  = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            // Bilinear value noise — coarse grid gives smooth low-frequency blobs
            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f); // smoothstep
                return lerp(
                    lerp(hash(i),               hash(i + float2(1, 0)), f.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x),
                    f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col      = tex2D(_MainTex, i.uv) * i.color;
                float  sprAlpha = col.a;

                // Isometric diagonal cuts — matches the actual neighbor directions:
                //   col+1 neighbor is at screen upper-right  → cut: uv.x + uv.y = 1  (keep > 1)  dir=0
                //   col-1 neighbor is at screen lower-left   → cut: uv.x + uv.y = 1  (keep < 1)  dir=1
                //   row+1 neighbor is at screen upper-left   → cut: uv.y - uv.x = 0  (keep > 0)  dir=2
                //   row-1 neighbor is at screen lower-right  → cut: uv.x - uv.y = 0  (keep > 0)  dir=3
                int dir = (int)round(_Direction);

                // Signed distance from the iso diagonal that splits this tile
                float d;
                if      (dir == 0) d =  (i.uv.x + i.uv.y) - 1.0; // upper-right: keep d > 0
                else if (dir == 1) d = -(i.uv.x + i.uv.y) + 1.0; // lower-left:  keep d > 0
                else if (dir == 2) d =  (i.uv.y - i.uv.x);        // upper-left:  keep d > 0
                else               d =  (i.uv.x - i.uv.y);        // lower-right: keep d > 0

                // Low-frequency smooth noise displaces the cut boundary
                float n = valueNoise(i.uv * _NoiseFreq + _NoiseSeed) * 2.0 - 1.0;
                float d_noise = d + n * _NoiseAmp;

                clip(d_noise); // discard pixels on the non-water side

                col.a   = sprAlpha;
                col.rgb *= sprAlpha; // premultiplied alpha

                return col;
            }
            ENDCG
        }
    }
}
