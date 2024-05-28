Shader "Custom/SlimeTowerDamageShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SlimeTex ("SlimeTex", 2D) = "white" {}
		_YOffset ("YOffset", float) = 1.0
        _SlimeColor ("SlimeColor", Color) = (1,0,0,1)		
	}
	SubShader
	{
		Tags
        {
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
        
            };
        
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                half2 texcoord  : TEXCOORD0;
            };

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _SlimeTex;
			fixed4 _SlimeColor;
			float _YOffset;
			
			v2f vert (appdata_t IN)
			{
			    
				v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color; 
				return OUT;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.texcoord);
				
				// if the texture is solid here, instead sample the slime texture
				// use scrolly bits
				if( col.a == 1 )
				{
    			    _YOffset %= 1.0f;

				    //map from a 160px wide image to a 32px wide one
				    float2 newUV = i.texcoord;
				    newUV.x = (newUV.x % 0.2f) / 0.2f;

                    //try to speed up the top drip (near 1.0f) and squish the bottom drip
                    newUV.y--;
                    newUV.y = newUV.y*newUV.y*newUV.y + 1.0f; 				    
                    
				    //map directly to a point on the slime texture. It is 32px tall, we are 80px tall, so our 1.0f = 2.5f there.
				    newUV.y *= 2.5f;
				    newUV.y += _YOffset;

                    newUV.y %= 1.0f;
				
				    col = tex2D(_SlimeTex, newUV);
				    col *= _SlimeColor;
				    col.a = 0.5f;
				}
				
				return col;
			}
			ENDCG
		}
	}
}
