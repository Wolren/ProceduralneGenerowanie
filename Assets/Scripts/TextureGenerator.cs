using System.Linq;
using UnityEngine;

/// <summary>
/// Klasa zawiera statyczną metodę TextureFromHeightMap,
/// która służy do generowania tekstury na podstawie mapy wysokości (HeightMap).
/// Metoda ta tworzy nowy obiekt Texture2D o wymiarach zgodnych z daną mapą wysokości
/// i koloruje go na podstawie wartości z mapy. Wartości te są normalizowane i interpolowane,
/// aby uzyskać kolor pomiędzy czarnym a białym, a następnie zapisywane są do tekstury.
/// Tekstura zwracana jest jako wynik metody.
/// </summary>
public static class TextureGenerator
{
    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);
        var black = Color.black;
        var white = Color.white;
        float minValue = heightMap.minValue;
        float maxValue = heightMap.maxValue;

        var colourMap = heightMap.values.Cast<float>()
            .Select(v => Mathf.InverseLerp(minValue, maxValue, v))
            .Select(t => Color.Lerp(black, white, t))
            .ToArray();

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 0
        };

        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }
}