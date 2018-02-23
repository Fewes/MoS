Shader "VeryLett/Cloth"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_NormalMap ("Normalmap", 2D) = "bump" {}
		_Color ("Color", color) = (1,1,1,0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 300

		Cull Back

		CGPROGRAM
			#pragma surface surf Standard nolightmap addshadow

			struct Input
			{
				float2 uv_MainTex;
			};

			fixed4 _Color;
			sampler2D _MainTex;
			sampler2D _NormalMap;

			void surf (Input IN, inout SurfaceOutputStandard o)
			{
				half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb;
				o.Alpha = c.a;
				o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
			}

		ENDCG
	}
	FallBack "Diffuse"
}