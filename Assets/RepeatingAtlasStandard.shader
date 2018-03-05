Shader "Custom/RepeatingAtlasStandard" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert finalcolor:mycolor fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.5

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex : TEXCOORD0;
			float4 color : COLOR;
			float4 atlasCoord : TEXCOORD1;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void vert(inout appdata_full v, out Input o)
		{
			o.uv_MainTex = v.texcoord;
			o.color = v.color;
			o.atlasCoord = v.texcoord1;
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		float2 mmod(float2 x, float2 m) {
			return fmod(fmod(x, m) + m, m);
		}

		void surf (Input i, inout SurfaceOutput o) {
			// xy = pos in atlas
			// zw = width / height in atlas
			fixed4 c = tex2D(_MainTex, mmod(i.uv_MainTex * i.atlasCoord.zw, i.atlasCoord.zw) + i.atlasCoord.xy);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}

		void mycolor(Input i, SurfaceOutput o, inout fixed4 color)
		{
			color *= i.color;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
