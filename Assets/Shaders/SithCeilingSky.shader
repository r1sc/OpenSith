// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Sith/CeilingSky"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CeilingSkyOffset ("Ceiling Sky Offset", Vector) = (0, 0, 0)
        _SkyHeight ("Sky Height", Float) = 0

        [Toggle(JKDF2_LIGHT_MODE)] _Ijim("JKDF2", Int) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Culling("Cull Mode", Int) = 2
        [Enum(NotDrawn,0,Vertex,1,Wireframe,2,Solid,3,Texture,4)] _GeoMode("Geometry Mode", Int) = 4
        [Enum(None,0,Lit,1,Diffuse,2,Gouraud,3)] _LightMode("Light Mode", Int) = 3

        [HideInInspector][Toggle(ZWRITE_ENABLED)] _ZWrite("", Int) = 1
    }

    CGINCLUDE
        #include "BaseShader.cginc"
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM
        //#pragma surface surf Lambert
        #pragma surface skysurf BakedLight vertex:vert finalcolor:fcolor nolightmap noambient

        float4 _CeilingSkyOffset;
        float _SkyHeight;
        half4 _MainTex_TexelSize;

        void skysurf (Input i, inout SithSurface o)
        {
            float3 skyNormal = float3(0.0, -1.0, 0.0); // Downward direction
            float2 newuv = CalculateCeilingSkyDistance(_MainTex_TexelSize.z, _MainTex_TexelSize.w, i.worldPos, _WorldSpaceCameraPos, skyNormal, _CeilingSkyOffset, _SkyHeight);

            // Sample the main texture
            o.Albedo = tex2D(_MainTex, newuv).rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}