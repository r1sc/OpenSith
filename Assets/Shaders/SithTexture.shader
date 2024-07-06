//Shader "Sith/TextureBak"
Shader "Sith/Texture"
{
    Properties
    {
        [Toggle(JKDF2_LIGHT_MODE)] _Ijim("JKDF2", Int) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Culling("Cull Mode", Int) = 2
        [Enum(NotDrawn,0,Vertex,1,Wireframe,2,Solid,3,Texture,4)] _GeoMode("Geometry Mode", Int) = 4
        [Enum(None,0,Lit,1,Diffuse,2,Gouraud,3)] _LightMode("Light Mode", Int) = 3

        [HideInInspector][Toggle(ZWRITE_ENABLED)] _ZWrite("", Int) = 1

        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _AmbientLight("Sector Ambient Light", Color) = (0,0,0,1)
        _ExtraLight("Sector Extra Light", Color) = (0,0,0,1)
        _Tint("Sector Tint", Color) = (0,0,0,1)
        _FaceExtraLight("Face Extra Light", Color) = (0,0,0,1)
    }

    CGINCLUDE
        #include "BaseShader.cginc"
    ENDCG

    SubShader{
        Tags {"Queue" = "Geometry" "RenderType"="Opaque" "IgnoreProjector"="True" "PreviewType"="Plane" }
        LOD 200
        Cull[_Culling]
        ZWrite[_ZWrite]
        ZTest LEqual

        CGPROGRAM

        #pragma surface surf BakedLight vertex:vert finalcolor:fcolor nolightmap noambient
        #pragma target 3.0

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        ENDCG
    }

    FallBack "Diffuse"
}
