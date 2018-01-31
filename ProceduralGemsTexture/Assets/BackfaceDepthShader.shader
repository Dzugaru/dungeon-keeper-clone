//-----------------------------------------------
//Renders linear depth of back faces of objects
//-----------------------------------------------
//Uses camera.depthTextureMode = DepthTextureMode.Depth; Unity feature,
//which does ShadowCaster pass (with Cull Front) first, takes Z buffer and puts it into _CameraDepthTexture,
//then regular (first) pass writes depth to Float RenderTexture

Shader "Custom/BackfaceDepthShader"
{
	Properties
	{
		
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "Gem" = "True"}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 pos : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 screenPos : TEXCOORD0;
			};

			sampler2D _CameraDepthTexture;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.screenPos = ComputeScreenPos(o.pos);
				return o;
			}

			float frag(v2f i) : SV_Target
			{
				float2 screenUV = i.screenPos.xy / i.screenPos.w;
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenUV));
				return depth;
			}
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ShadowCaster" }

			Cull Front

			CGPROGRAM
			#pragma target 3.0				

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster

			#include "UnityCG.cginc"

			struct v2f
			{
				V2F_SHADOW_CASTER;
			};

			v2f vert(appdata_full v)
			{
				v2f o;
				TRANSFER_SHADOW_CASTER(o)
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
