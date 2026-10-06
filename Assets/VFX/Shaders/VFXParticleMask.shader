// Lightweight Built-in pipeline particle shader for the stylized particle pack.
// The pack's own materials use a URP Shader Graph, which does not exist in this Built-in project.
// The pack texture stores the cartoon shapes in its R channel, so R is the alpha mask and the
// particle's vertex colour tints it. One texture fetch, no fog, no lighting, no soft-particle depth read.
Shader "OfficeFever/VFX/ParticleMask"
{
    Properties
    {
        _MainTex ("Shape Texture (R = mask)", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4 // LEqual (8 = Always, draws over the character)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Tint;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed mask = tex2D(_MainTex, i.uv).r;
                return fixed4(i.color.rgb, mask * i.color.a);
            }
            ENDCG
        }
    }
}
