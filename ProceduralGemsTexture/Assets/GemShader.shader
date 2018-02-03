Shader "Custom/GemShader"
{
	Properties
	{
		_BackFaceDepthTex("Albedo", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Gem" = "True"}			

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag						
			

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;				
			};

			struct v2f
			{
				float4 screenPos : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			sampler2D _CameraDepthTexture;
			sampler2D _BackFaceDepthTex;
		
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);	
				o.screenPos = ComputeScreenPos(o.pos);
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{	
				float backDepth = tex2Dproj(_BackFaceDepthTex, i.screenPos).r;
				float depth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, i.screenPos));

				return float4((backDepth - depth) / 4, 0, 0, 1);
			}
			ENDCG
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}			

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
