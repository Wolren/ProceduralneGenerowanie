using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class TextureData : UpdatableData
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;
    public Layer[] layers;
    private float m_SavedMinHeight;
    private float m_SavedMaxHeight;
    private static readonly int _layerCount = Shader.PropertyToID("layerCount");
    private static readonly int _baseColours = Shader.PropertyToID("baseColours");
    private static readonly int _baseStartHeights = Shader.PropertyToID("baseStartHeights");
    private static readonly int _baseBlends = Shader.PropertyToID("baseBlends");
    private static readonly int _baseColourStrength = Shader.PropertyToID("baseColourStrength");
    private static readonly int _baseTextureScales = Shader.PropertyToID("baseTextureScales");
    private static readonly int _baseTextures = Shader.PropertyToID("baseTextures");
    private static readonly int _minHeight = Shader.PropertyToID("minHeight");
    private static readonly int _maxHeight = Shader.PropertyToID("maxHeight");

    public void ApplyToMaterial(Material material)
    {
        material.SetInt(_layerCount, layers.Length);
        material.SetColorArray(_baseColours, layers.Select(x => x.odcień).ToArray());
        material.SetFloatArray(_baseStartHeights, layers.Select(x => x.początkowaWysokość).ToArray());
        material.SetFloatArray(_baseBlends, layers.Select(x => x.siłaWymieszania).ToArray());
        material.SetFloatArray(_baseColourStrength, layers.Select(x => x.siłaOdcienia).ToArray());
        material.SetFloatArray(_baseTextureScales, layers.Select(x => x.skalaTekstury).ToArray());
        var texturesArray = GenerateTextureArray(layers.Select(x => x.tekstura).ToArray());
        material.SetTexture(_baseTextures, texturesArray);
        UpdateMeshHeights(material, m_SavedMinHeight, m_SavedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        m_SavedMinHeight = minHeight;
        m_SavedMaxHeight = maxHeight;
        material.SetFloat(_minHeight, minHeight);
        material.SetFloat(_maxHeight, maxHeight);
    }

    private static Texture2DArray GenerateTextureArray(IReadOnlyList<Texture2D> textures)
    {
        var textureArray = new Texture2DArray(textureSize, textureSize, textures.Count, textureFormat, true);
        for (int i = 0; i < textures.Count; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }

        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D tekstura;
        public Color odcień;
        [Range(0, 1)] public float siłaOdcienia;
        [Range(0, 1)] public float początkowaWysokość;
        [Range(0, 1)] public float siłaWymieszania;
        public float skalaTekstury;
    }
}