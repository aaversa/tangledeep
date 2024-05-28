// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/SlimeShader" {
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
    	_Color("Tint", Color) = (1,1,1,1)
		_TileLocation("TileLocation", Vector) = (0,0,0,0)
		_PulseTime("Time", Float) = 0.0
		_PulseMod("Frequency", Float) = 4.0
		_TileSizeX("TileSizeX", Float) = 0.0
		_TileSizeY("TileSizeY", Float) = 0.0
		
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
#pragma multi_compile _ PIXELSNAP_ON
#pragma multi_compile _ GRAYSCALE_ON GRAYSCALE_OFF
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
    
        fixed4 _Color;
        
        float4 _TileLocation;
        float4 _PulseOrigin1;
        float4 _PulseOrigin2;
        float _PulseTime;
        float _PulseMod;
        float _TileSizeX;
        float _TileSizeY;
        float4 _OriginArray[8];
    
        v2f vert(appdata_t IN)
        {
            v2f OUT;
            OUT.vertex = UnityObjectToClipPos(IN.vertex);
            OUT.texcoord = IN.texcoord;
            OUT.color = IN.color * _Color;
            OUT.vertex = UnityPixelSnap(OUT.vertex);
    
            return OUT;
        }
    
        sampler2D _MainTex;
        
        float GetSaturatedValueForOrigin( float4 tileLoc, fixed2 partialUV )
        {
            //if( origin.x == origin.y == 0 )
            //    return 0;
            float bestDistance = 9999;
            
            for(int t=0; t < 8; t++)
            {
                fixed2 delta;
                
                delta.x = (tileLoc.x + partialUV.x ) - _OriginArray[t].x;
                delta.y = (tileLoc.y + partialUV.y ) - _OriginArray[t].y;
                
                bestDistance = min( sqrt(delta.x * delta.x + delta.y * delta.y), bestDistance);
            }
                
            //distance += 0.2f * sin((_PulseTime * 2.4f) % 6.28f);
            
            float truePulse = (_PulseTime - bestDistance) % _PulseMod;
            float saturatedValue = (1.4f - min(truePulse, 1.4f)) / 1.4f;
            
            saturatedValue /= 0.9f;
            saturatedValue -= max(0, (saturatedValue - 1.0f) / 0.12f);
            
            return saturatedValue;
        }	
    
        fixed4 frag(v2f IN) : SV_Target
        {
            fixed2 partialUV = IN.texcoord;
            
            //Calculate the exact world position of this pixel, and use that to determine distance from towers.
            partialUV.x = (partialUV.x % _TileSizeX) / _TileSizeX;
            partialUV.y = (partialUV.y % _TileSizeY) / _TileSizeY;
    
            //Check all the tower locations, find the closest one and use that to generate visual wave info.      
            float aggregateValue = GetSaturatedValueForOrigin(_TileLocation, partialUV);
            
            //get the raw pixel value -- read in from _MainTex which is the mud texture, and the texcoords are
            //0-1 values for location on that main texture;
            fixed4 c = tex2D(_MainTex, IN.texcoord); // * IN.color;
            
            //change it to the color we've sent in, based on luminosity
            fixed avg = c.r * 0.21f + c.g * 0.72f + c.b * 0.07f;
            c.rgb = avg * c.a * IN.color;
            
            //amplify it for the wave we're sending around.
            c.rgb *= 1.0 + aggregateValue;
            return c;
        }
        
		ENDCG
	}
	}
}
