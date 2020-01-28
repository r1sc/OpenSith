Shader "Unlit/VertexNormal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal: NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 normal: NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
                return o;
            }

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = fixed4((i.normal * 0.5 + 0.5).xyz, 1);
                return col;
            }
            ENDCG
        }
    }
}
