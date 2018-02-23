Shader "VeryLett/Cloth2"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_SmoothnessTex ("Smoothness", 2D) = "white" {}
		_NormalMap ("Normalmap", 2D) = "bump" {}
		_Color ("Color", color) = (1,1,1,0)
		_Smoothness ("Smoothness", Range(0, 1)) = 0.05
		_HalfLambert ("Half Lambert", Range(0, 1)) = 0

		_RimIntensity ("Rim Intensity", Range(0, 1)) = 0.5
		_RimExp ("Rim Exponent", Range(1, 16)) = 4
		_Backside ("Backside Lighting", Range(0, 1)) = 0.25
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
			#include "UnityPBSLighting.cginc"
			#include "AutoLight.cginc"

			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))

			struct Input
			{
				float2 uv_MainTex;
			};

			fixed4 		_Color;
			sampler2D 	_MainTex;
			sampler2D	_SmoothnessTex;
			sampler2D 	_NormalMap;
			float		_Smoothness;
			float		_RimIntensity;
			float		_RimExp;
			float		_Backside;
			float		_HalfLambert;

			void surf (Input IN, inout SurfaceOutputStandard o)
			{
				half4 c 		= tex2D (_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo 		= c.rgb;
				o.Alpha 		= c.a;
				o.Normal 		= UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
				o.Metallic		= 0;
				o.Smoothness	= tex2D (_SmoothnessTex, IN.uv_MainTex).r * _Smoothness;
			}

			half4 BRDF_Cloth (half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
			    half3 normal, half3 viewDir,
			    UnityLight light, UnityIndirect gi)
			{
			    half perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
			    half3 halfDir = Unity_SafeNormalize (light.dir + viewDir);

			// NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
			// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
			// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
			// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
			// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
			// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
			#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0

			#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
			    // The amount we shift the normal toward the view vector is defined by the dot product.
			    half shiftAmount = dot(normal, viewDir);
			    normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
			    // A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
			    //normal = normalize(normal);

			    half nv = saturate(dot(normal, viewDir)); // TODO: this saturate should no be necessary here
			#else
			    half nv = abs(dot(normal, viewDir));    // This abs allow to limit artifact
			#endif

			    half nl = saturate(dot(normal, light.dir));
			    half nlh = saturate(dot(normal, light.dir)*0.5+0.5);
			    nl = lerp(nl, nlh, _HalfLambert);
			    half nh = saturate(dot(normal, halfDir));

			    half lv = saturate(dot(light.dir, viewDir));
			    half lh = saturate(dot(light.dir, halfDir));

			    // Diffuse term
			    half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;

			    // Specular term
			    // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
			    // BUT 1) that will make shader look significantly darker than Legacy ones
			    // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
			    half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
			#if UNITY_BRDF_GGX
			    // GGX with roughtness to 0 would mean no specular at all, using max(roughness, 0.002) here to match HDrenderloop roughtness remapping.
			    roughness = max(roughness, 0.002);
			    half V = SmithJointGGXVisibilityTerm (nl, nv, roughness);
			    half D = GGXTerm (nh, roughness);
			#else
			    // Legacy
			    half V = SmithBeckmannVisibilityTerm (nl, nv, roughness);
			    half D = NDFBlinnPhongNormalizedTerm (nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
			#endif

			    half specularTerm = V*D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

			#   ifdef UNITY_COLORSPACE_GAMMA
			        specularTerm = sqrt(max(1e-4h, specularTerm));
			#   endif

			    // specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
			    specularTerm = max(0, specularTerm * nl);
			#if defined(_SPECULARHIGHLIGHTS_OFF)
			    specularTerm = 0.0;
			#endif

			    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
			    half surfaceReduction;
			#   ifdef UNITY_COLORSPACE_GAMMA
			        surfaceReduction = 1.0-0.28*roughness*perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
			#   else
			        surfaceReduction = 1.0 / (roughness*roughness + 1.0);           // fade \in [0.5;1]
			#   endif

			    // To provide true Lambert lighting, we need to be able to kill specular completely.
			    specularTerm *= any(specColor) ? 1.0 : 0.0;

			    half grazingTerm = saturate(smoothness + (1-oneMinusReflectivity));
			    half3 color =   diffColor * (gi.diffuse + light.color * diffuseTerm)
			                    + specularTerm * light.color * FresnelTerm (specColor, lh)
			                    + surfaceReduction * gi.specular * FresnelLerp (specColor, grazingTerm, nv);

			    return half4(color, 1);
			}

			inline half4 LightingCloth(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
			{
			    s.Normal = normalize(s.Normal);

			    half oneMinusReflectivity;
			    half3 specColor;
			    s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

			    // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
			    // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
			    half outputAlpha;
			    s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

			    half4 c = BRDF_Cloth (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
			    c.a = outputAlpha;
			    return c;
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
				fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
				#ifdef UNITY_COMPILER_HLSL
				SurfaceOutputStandard o = (SurfaceOutputStandard)0;
				#else
				SurfaceOutputStandard o;
				#endif
				o.Albedo = 0.0;
				o.Emission = 0.0;
				o.Alpha = 0.0;
				o.Occlusion = 1.0;
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
				giInput.worldViewDir = worldViewDir;
				giInput.atten = atten;
				giInput.lightmapUV = 0.0;

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

				LightingStandard_GI(o, giInput, gi);

				// realtime lighting: call lighting function
				c += LightingCloth (o, worldViewDir, gi);

				float transRim = dot(o.Normal, worldViewDir);
				transRim = pow(transRim, 2);

				o.Normal *= -1;
				gi.indirect.diffuse *= 0;
				gi.indirect.specular *= 0;
				c += LightingCloth (o, worldViewDir, gi) * transRim * _Backside;
				o.Normal *= -1;


				UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
				UNITY_OPAQUE_ALPHA(c.a);

				float3 reflDir = reflect(-worldViewDir, o.Normal);
				float4 envSample = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflDir, (1-o.Smoothness) * 6);
				float3 cube = DecodeHDR(envSample, unity_SpecCube0_HDR);

				float rim = 1-dot(o.Normal, worldViewDir);
				rim = pow(rim, _RimExp);

				c.rgb += cube * rim * _RimIntensity;

				// c.rgb = cube;

				return c;
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

				#define UNITY_PASS_FORWARDBASE

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
				SurfaceOutputStandard o = (SurfaceOutputStandard)0;
				#else
				SurfaceOutputStandard o;
				#endif
				o.Albedo = 0.0;
				o.Emission = 0.0;
				o.Alpha = 0.0;
				o.Occlusion = 1.0;
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