Shader "Custom/WaterShader"
{
    Properties
    {
        [Header(Smoothness)]
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [Header(Colors)]
        _MapaGradiente("Gradient map", 2D) = "white" {}
        _Costa("Shore color", Color) = (1,1,1,1)
        _CostaThreshold("Shore color threshold", Range(0, 1)) = 0
        [HDR]_Emission("Emission", Color) = (1,1,1,1)

        [Header(Tessellation)]
        _VectorLength("Vector length", Range(0.0001, 0.2)) = 0.1
        _MaxTessellationDistance("Max tessellation distance", float) = 100
        _Tessellation("Tessellation", Range(1.0, 128.0)) = 1.0

        [Header(Vertex Offset)]
        _NoiseTextureA("Noise texture A", 2D) = "white" {}
        _NoiseAProperties("Properties A (speedX, speedY, contrast, contribution)", Vector) = (0,0,1,1)
        _NoiseTextureB("Noise texture B", 2D) = "white" {}
        _NoiseBProperties("Properties B (speedX, speedY, contrast, contribution)", Vector) = (0,0,1,1)
        _OffsetAmount("Offset amount", Range(0.0, 1.0)) = 1.0
        _MinOffset("Min offset", Range(0.0, 1.0)) = 0.2


        [Header(Displacement)]
        _DisplacementGuide("Displacement guide", 2D) = "white" {}
        _DisplacementProperties("Displacement properties (speedX, speedY, contribution)", Vector) = (0,0,0,0)
        _DispMark("DispMark", Range(0,1)) = 0.5
        _Splat("SplatMap", 2D) = "black" {}
        _SeaMarkTexture("Sea Mark texture", 2D) = "white" {}
        _SeaMarkColor("Sea Mark color", Color) = (1,1,1,1) 

        [Header(Shore and foam)]
        _ShoreIntersectionThreshold("Shore intersection threshold", float) = 0
        _FoamTexture("Foam texture", 2D) = "white" {} 
        _FoamProperties("Foam properties (speedX, speedY, threshold, threshold smoothness)", Vector) = (0,0,0,0)
        _FoamIntersectionProperties("Foam intersection properties (intersection threshold, foam threshold, threshold smoothness, cutoff)", Vector) = (0,0,0,0)

        [Header(Transparency)]
        _CorteTransparenciaMin("Corte Trasparencia Min", float) = 0.6
        _CorteTransparenciaMax("Corte Transparencia", float) = 0.4

    }
    SubShader
    {
        
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
        Blend One OneMinusSrcAlpha
        ZWrite Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert tessellate:tessDistance alpha:fade addshadow
        #pragma require tessellation tessHW
        #include "Tessellation.cginc"
        #pragma target 3.0
        
        
        sampler2D _Splat;
        float _DispMark;
        sampler2D _SeaMarkTexture;
        fixed4 _SeaMarkColor;

        half _Smoothness;
        float _SmoothnessFresnel;

        sampler2D _MapaGradiente;
        fixed4 _Costa;
        float _CostaThreshold;
        fixed4 _Emission;
      
        float _VectorLength;
        float _MaxTessellationDistance;
        float _Tessellation;

        sampler2D _NoiseTextureA;
        float4 _NoiseTextureA_ST;
        float4 _NoiseAProperties;
        
        sampler2D _NoiseTextureB;
        float4 _NoiseTextureB_ST;
        float4 _NoiseBProperties;
        float _OffsetAmount;
        float _MinOffset;
        
        float4 _DisplacementProperties;
        sampler2D _DisplacementGuide;
        float4 _DisplacementGuide_ST;

        float _ShoreIntersectionThreshold;
        sampler2D _FoamTexture;
        float4 _FoamProperties;
        float4 _FoamTexture_ST;
        float4 _FoamIntersectionProperties;

        float _CorteTransparenciaMax;
        float _CorteTransparenciaMin;

        sampler2D _CameraDepthTexture;

        
        struct Input
        {
            float4 color: Color;
            float3 worldPos;
            float4 screenPos;
            float2 uv_SeaMarkTexture;
            float2 uv_Splat;
        };

        float4 tessDistance (appdata_full v0, appdata_full v1, appdata_full v2) {
            float minDist = 10.0;
            float maxDist = _MaxTessellationDistance;
            return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tessellation);
        }

        float sampleNoiseTexture(float2 pos, sampler2D noise, float4 props, float2 scale, float2 displ) {
            float value = tex2Dlod(noise, float4(pos * scale + displ + _Time.y * props.xy, 0.0, 0.0));
            value = (saturate(lerp(0.5, value, props.z)) * 2.0 - 1.0) * props.w;
            return value;
        }

        float noiseOffset(float2 pos) {
            float2 displ = tex2Dlod(_DisplacementGuide, float4(pos * _DisplacementGuide_ST.xy + _Time.y * _DisplacementProperties.xy, 0.0, 0.0)).xy;
            displ = ((displ * 2.0) - 1.0) * _DisplacementProperties.z;
            float noiseA = sampleNoiseTexture(pos, _NoiseTextureA, _NoiseAProperties, _NoiseTextureA_ST.xy, displ);
            float noiseB = sampleNoiseTexture(pos, _NoiseTextureB, _NoiseBProperties, _NoiseTextureB_ST.xy, displ);
            return noiseA * noiseB;
        }
        


        #define smootherstep(x)saturate(x * x * x * (x * (6 * x - 15) + 10))
        #define remap(s)((s + 1.0) / 2.0)


        

        void vert(inout appdata_full v) {
            float4 v0 = v.vertex;
            float4 v1 = v0 + float4(_VectorLength, 0.0, 0.0, 0.0);
            float4 v2 = v0 + float4(0.0, 0.0, _VectorLength, 0.0);

            float4 screenPos = ComputeScreenPos(UnityObjectToClipPos(v0.xyz));
            float depth = LinearEyeDepth(tex2Dlod(_CameraDepthTexture, float4(screenPos.xy / screenPos.w, 0.0, 0.0)));
            float diff = smootherstep(saturate((depth - screenPos.w) / _ShoreIntersectionThreshold));
            float thresDiff = max(_MinOffset, diff);
            float factor = thresDiff * _OffsetAmount;

            float vertexOffset = noiseOffset(mul(unity_ObjectToWorld, v0).xz); 

            v0.y += vertexOffset * factor;
            v1.y += noiseOffset(mul(unity_ObjectToWorld, v1).xz) * factor;
            v2.y += noiseOffset(mul(unity_ObjectToWorld, v2).xz) * factor;

            float3 vn = cross(v2.xyz - v0.xyz, v1.xyz - v0.xyz); //Calcular onde o objeto cruza outros objetos, reduzindo o deslocamento dos vértices nessas áreas.
            v.normal = normalize(vn); 

            v.vertex = v0;
            v.color = fixed4(remap(vertexOffset).xxxx);

            //Marks
            float d = tex2Dlod(_Splat, float4(v.texcoord.xy,0,0)).r * _DispMark;
            v.vertex.xyz -= v.normal * d;
            v.vertex.xyz += v.normal * _DispMark;
        }

        //void disp(inout appdata_full v) // Input e output do appdata
        //{
        //    
        //}
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            //Displacement
            float2 displ = tex2D(_DisplacementGuide, IN.worldPos.xz * _DisplacementGuide_ST.xy + _Time.y * _DisplacementProperties.xy).xy;
            displ = ((displ * 2.0) - 1.0) * _DisplacementProperties.z;

            //Espuma
            float foamTex = tex2D(_FoamTexture, IN.worldPos.xz * _FoamTexture_ST.xy + displ + sin(_Time.y) * _FoamProperties.xy);
            float foam = saturate(foamTex - smoothstep(_FoamProperties.z + _FoamProperties.w, _FoamProperties.z, IN.color.x));

            //Calculos de profundidade
            float depth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos));
            float shoreDepth = smoothstep(0.0, _CostaThreshold, Linear01Depth(depth));
            depth = LinearEyeDepth(depth);
            float foamDiff = smootherstep(saturate((depth - IN.screenPos.w) / _FoamIntersectionProperties.x));
            float shoreDiff = smootherstep(saturate((depth - IN.screenPos.w) / _ShoreIntersectionThreshold));
            float transparencyDiff = smootherstep(saturate((depth - IN.screenPos.w) / lerp(_CorteTransparenciaMin, _CorteTransparenciaMax, remap(sin(_Time.y + UNITY_PI / 2.0)))));
                        
            //Shore
            float shoreFoam = saturate(foamTex - smoothstep(_FoamIntersectionProperties.y - _FoamIntersectionProperties.z, _FoamIntersectionProperties.y, foamDiff) + _FoamIntersectionProperties.w * (1.0 - foamDiff));
            foam += shoreFoam;
            
            //Colors
            o.Albedo = lerp(lerp(fixed3(0.0, 0.0, 0.0), _Costa.rgb, 1), tex2D(_MapaGradiente, float2(IN.color.x, 0.0)).rgb, shoreDepth) + foam;
            //o.Emission = o.Albedo * saturate(_WorldSpaceLightPos0.y) * _LightColor0 * _Emission;

            //Smoothness
            o.Smoothness = _Smoothness * foamDiff;

            o.Alpha = saturate(lerp(1.0, lerp(0.5, _Costa.a, 1), 1.0 - shoreDiff) * transparencyDiff);
        }
        ENDCG
    }
    FallBack "Diffuse"
}