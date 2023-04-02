/*
Shader posiada dwie właściwości: testTexture i testScale, które określają teksturę i skalę testową.
Wewnątrz shadera zdefiniowane są zmienne przechowujące informacje o liczbie warstw terenu, kolorach bazowych,
początkowych wysokościach, mieszankach, siłach koloru bazowego, skalach tekstur bazowych,
minimalnej i maksymalnej wysokości terenu oraz teksturze testowej.

Funkcja surf(Input IN, inout SurfaceOutputStandard o) jest główną funkcją,
która określa, jak teren ma być renderowany. Dla każdej warstwy terenu,
funkcja ta oblicza siłę rysowania warstwy i miesza kolor bazowy z kolorem tekstury,
używając funkcji lerp. Funkcja triplanar służy do projektowania tekstur w trójwymiarowym terenie,
a funkcja inverse_lerp zwraca wartość między dwoma wartościami w przedziale 0-1 na podstawie wartości wejściowej.
 */
Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture", 2D) = "white"{}
        testScale("Scale", Float) = 1
    }
    
    SubShader
    {
        Tags
        { 
            "RenderType"="Opaque" 
        }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;
        int layerCount;
        float3 baseColours[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColourStrength[maxLayerCount];
        float baseTextureScales[maxLayerCount];
        float minHeight;
        float maxHeight;
        sampler2D testTexture;
        float testScale;
        UNITY_DECLARE_TEX2DARRAY(baseTextures);
        
        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };
        
          float inverse_lerp(float a, float b, float value)
        {
            return saturate((value - a) / (b - a));
        }
        
        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex)
        {
            float3 scaled_world_pos = worldPos / scale;
            const float3 x_projection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaled_world_pos.y, scaled_world_pos.z, textureIndex)) * blendAxes.x;
            const float3 y_projection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaled_world_pos.x, scaled_world_pos.z, textureIndex)) * blendAxes.y;
            const float3 z_projection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaled_world_pos.x, scaled_world_pos.y, textureIndex)) * blendAxes.z;
            return x_projection + y_projection + z_projection;
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            const float height_percent = inverse_lerp(minHeight, maxHeight, IN.worldPos.y);
            const float3 blend_axes = abs(IN.worldNormal) / dot(abs(IN.worldNormal), 1);
            for (int i = 0; i < layerCount; i++)
            {
                const float draw_strength = inverse_lerp(-baseBlends[i] / 2 - epsilon, baseBlends[i] / 2, height_percent - baseStartHeights[i]);
                const float3 base_colour = baseColours[i] * baseColourStrength[i];
                const float3 texture_colour = triplanar(IN.worldPos, baseTextureScales[i], blend_axes, i) * (1 - baseColourStrength[i]);
                o.Albedo = lerp(o.Albedo, base_colour + texture_colour, draw_strength);
            }
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}
