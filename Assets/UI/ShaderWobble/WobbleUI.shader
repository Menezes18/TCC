Shader "Custom/WobbleUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Main Texture", 2D) = "white" {}
        _FlowMap ("Flow Map", 2D) = "white" {}
        _Strength ("Strength", Range(0, 0.1)) = 0.005
        _Speed ("Speed", Range(0, 10)) = 4.0
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            sampler2D _FlowMap;
            float4 _MainTex_ST;
            float4 _FlowMap_ST;
            float _Strength;
            float _Speed;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                float time = _Time.y * _Speed;
                
                float timeModulo = fmod(time, 4.0);
                
                float timeFloor = floor(timeModulo / 4.0);
                
                float timeNormalized = timeFloor / 4.0;
                
                float2 flowUV = IN.texcoord;
                
                float4 flowSample = tex2D(_FlowMap, flowUV);
                float3 flowNormal = UnpackNormal(flowSample);
                
                float2 flowVector = flowNormal.xy * _Strength;
                
                float2 offset = flowVector * timeNormalized * 2.0;
                
                float2 finalUV = IN.texcoord + offset;
                
                half4 color = (tex2D(_MainTex, finalUV) + _TextureSampleAdd) * IN.color;
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                // Clip baseado no alpha (mantém transparência)
                clip(color.a - 0.001);
                
                return color;
            }
            ENDCG
        }
    }
    
    Fallback "UI/Default"
}
