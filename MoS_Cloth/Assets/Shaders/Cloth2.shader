Shader "VeryLett/Cloth2"
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

		Cull Off

		CGINCLUDE
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase noshadowmask nodynlightmap nolightmap
			#include "HLSLSupport.cginc"
			#include "UnityShaderVariables.cginc"
			#include "UnityShaderUtilities.cginc"

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))

			struct Input
			{
				float2 uv_MainTex;
			};

			fixed4 _Color;
			sampler2D _MainTex;
			sampler2D _NormalMap;

			void surf (Input IN, inout SurfaceOutput o)
			{
				half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb;
				o.Alpha = c.a;
				o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
			}

			struct v2f_surf
			{
				UNITY_POSITION(pos);
				float2 pack0 : TEXCOORD0; // _MainTex
				float4 tSpace0 : TEXCOORD1;
				float4 tSpace1 : TEXCOORD2;
				float4 tSpace2 : TEXCOORD3;
				#if UNITY_SHOULD_SAMPLE_SH
					half3 sh : TEXCOORD4; // SH
				#endif
				UNITY_SHADOW_COORDS(5)
				UNITY_FOG_COORDS(6)
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			float4 _MainTex_ST;

			// vertex shader
			v2f_surf vert_surf (appdata_full v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				v2f_surf o;
				UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
				UNITY_TRANSFER_INSTANCE_ID(v,o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
				fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
				o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

				#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
					o.sh = 0;
					// Approximated illumination from non-important point lights
					#ifdef VERTEXLIGHT_ON
						o.sh += Shade4PointLights (
						unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
						unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
						unity_4LightAtten0, worldPos, worldNormal);
					#endif
					o.sh = ShadeSHPerVertex (worldNormal, o.sh);
				#endif

				UNITY_TRANSFER_SHADOW(o,v.texcoord1.xy); // pass shadow coordinates to pixel shader
				UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
				return o;
			}
		ENDCG

		// ---- forward rendering base pass:
		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
				// compile directives
				#pragma vertex vert_surf
				#pragma fragment frag_surf

				#define UNITY_PASS_FORWARDBASE

				// fragment shader
				fixed4 frag_surf (v2f_surf IN, fixed facing : VFACE) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(IN);
				  // prepare and unpack data
					Input surfIN;
					UNITY_INITIALIZE_OUTPUT(Input,surfIN);
					surfIN.uv_MainTex.x = 1.0;
					surfIN.uv_MainTex = IN.pack0.xy;
					float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
				  #ifndef USING_DIRECTIONAL_LIGHT
					fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
				  #else
					fixed3 lightDir = _WorldSpaceLightPos0.xyz;
				  #endif
				  #ifdef UNITY_COMPILER_HLSL
					SurfaceOutput o = (SurfaceOutput)0;
				  #else
					SurfaceOutput o;
				  #endif
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;
					fixed3 normalWorldVertex = fixed3(0,0,1);
					o.Normal = fixed3(0,0,1);

				  // call surface function
					surf (surfIN, o);

				  // compute lighting & shadowing factor
					UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
					fixed4 c = 0;
					fixed3 worldN;
					worldN.x = dot(IN.tSpace0.xyz, o.Normal);
					worldN.y = dot(IN.tSpace1.xyz, o.Normal);
					worldN.z = dot(IN.tSpace2.xyz, o.Normal);
					worldN = normalize(worldN);
					o.Normal = worldN * facing;

				  // Setup lighting environment
					UnityGI gi;
					UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
					gi.indirect.diffuse = 0;
					gi.indirect.specular = 0;
					gi.light.color = _LightColor0.rgb;
					gi.light.dir = lightDir;
				  // Call GI (lightmaps/SH/reflections) lighting function
					UnityGIInput giInput;
					UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
					giInput.light = gi.light;
					giInput.worldPos = worldPos;
					giInput.atten = atten;
				  #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
					giInput.lightmapUV = IN.lmap;
				  #else
					giInput.lightmapUV = 0.0;
				  #endif
				  #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
					giInput.ambient = IN.sh;
				  #else
					giInput.ambient.rgb = 0.0;
				  #endif
					giInput.probeHDR[0] = unity_SpecCube0_HDR;
					giInput.probeHDR[1] = unity_SpecCube1_HDR;
				  #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
				    giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
				  #endif
				  #ifdef UNITY_SPECCUBE_BOX_PROJECTION
				    giInput.boxMax[0] = unity_SpecCube0_BoxMax;
				    giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
				    giInput.boxMax[1] = unity_SpecCube1_BoxMax;
				    giInput.boxMin[1] = unity_SpecCube1_BoxMin;
				    giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
				  #endif
				    LightingLambert_GI(o, giInput, gi);

				  // realtime lighting: call lighting function
				    c += LightingLambert (o, gi);
				  UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
				  UNITY_OPAQUE_ALPHA(c.a);
				  return c;
				}
			ENDCG
		}

		// ---- forward rendering additive lights pass:
		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardAdd" }
			ZWrite Off Blend One One

			CGPROGRAM
				// compile directives
				#pragma vertex vert_surf
				#pragma fragment frag_surf
				#pragma multi_compile_instancing
				#pragma multi_compile_fog
				#pragma skip_variants INSTANCING_ON
				#pragma multi_compile_fwdadd noshadowmask nodynlightmap nolightmap

				// fragment shader
				fixed4 frag_surf (v2f_surf IN, fixed facing : VFACE) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(IN);
					// prepare and unpack data
					Input surfIN;
					UNITY_INITIALIZE_OUTPUT(Input,surfIN);
					surfIN.uv_MainTex.x = 1.0;
					surfIN.uv_MainTex = IN.pack0.xy;
					float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
					#ifndef USING_DIRECTIONAL_LIGHT
						fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
					#else
						fixed3 lightDir = _WorldSpaceLightPos0.xyz;
					#endif
					#ifdef UNITY_COMPILER_HLSL
						SurfaceOutput o = (SurfaceOutput)0;
					#else
						SurfaceOutput o;
					#endif
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;
					fixed3 normalWorldVertex = fixed3(0,0,1);
					o.Normal = fixed3(0,0,1);

					// call surface function
					surf (surfIN, o);
					UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
					fixed4 c = 0;
					fixed3 worldN;
					worldN.x = dot(IN.tSpace0.xyz, o.Normal);
					worldN.y = dot(IN.tSpace1.xyz, o.Normal);
					worldN.z = dot(IN.tSpace2.xyz, o.Normal);
					worldN = normalize(worldN);
					o.Normal = worldN;

					// Setup lighting environment
					UnityGI gi;
					UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
					gi.indirect.diffuse = 0;
					gi.indirect.specular = 0;
					gi.light.color = _LightColor0.rgb;
					gi.light.dir = lightDir;
					gi.light.color *= atten;
					c += LightingLambert (o, gi);
					c.a = 0.0;
					UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
					UNITY_OPAQUE_ALPHA(c.a);
					return c;
				}
			ENDCG
		}

		// ---- shadow caster pass:
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			// compile directives
			#pragma vertex vert_shadow
			#pragma fragment frag_shadow

					

			// vertex-to-fragment interpolation data
			struct v2f_shadow
			{
				V2F_SHADOW_CASTER;
				float3 worldPos : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// vertex shader
			v2f_shadow vert_shadow (appdata_full v)
			{
			  UNITY_SETUP_INSTANCE_ID(v);
			  v2f_shadow o;
			  UNITY_INITIALIZE_OUTPUT(v2f_shadow,o);
			  UNITY_TRANSFER_INSTANCE_ID(v,o);
			  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			  fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
			  o.worldPos = worldPos;
			  TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
			  return o;
			}

			// fragment shader
			fixed4 frag_shadow (v2f_shadow IN, fixed facing : VFACE) : SV_Target
			{
			  UNITY_SETUP_INSTANCE_ID(IN);
			  // prepare and unpack data
			  Input surfIN;
			  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
			  surfIN.uv_MainTex.x = 1.0;
			  float3 worldPos = IN.worldPos;
			  #ifndef USING_DIRECTIONAL_LIGHT
			    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
			  #else
			    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
			  #endif
			  #ifdef UNITY_COMPILER_HLSL
			  SurfaceOutput o = (SurfaceOutput)0;
			  #else
			  SurfaceOutput o;
			  #endif
			  o.Albedo = 0.0;
			  o.Emission = 0.0;
			  o.Specular = 0.0;
			  o.Alpha = 0.0;
			  o.Gloss = 0.0;
			  fixed3 normalWorldVertex = fixed3(0,0,1) * facing;

			  // call surface function
			  surf (surfIN, o);
			  SHADOW_CASTER_FRAGMENT(IN)
			}

			ENDCG

		}
	}
	FallBack "Diffuse"
}