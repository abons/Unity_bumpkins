Shader "Bumpkins/SpriteAdditiveTint"
{
    Properties
    {
        _MainTex   ("Sprite Texture", 2D)    = "white" {}
        _Color     ("Multiply Tint",  Color) = (1,1,1,1)
        _AddColor  ("Fall Target Color", Color) = (0,0,0,0)
        // _AddColor.rgb = target colour to blend toward; _AddColor.a = blend strength (0=original, 1=full target)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4    _Color;
            fixed4    _AddColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                // Lerp toward target colour — works for both darker and lighter targets
                col.rgb = lerp(col.rgb, _AddColor.rgb, _AddColor.a);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
