using UnityEngine;

/// <summary>
/// Klasa zawiera metodę GenerateHeightMap, która generuje mapę wysokości z zadanymi ustawieniami.
/// Mapa wysokości jest generowana na podstawie szumu Perlin,
/// a następnie wykorzystuje krzywą wysokości i mnożnik wysokości, aby uzyskać końcowe wartości wysokości.
/// Metoda zwraca strukturę HeightMap, która składa się z tablicy wartości wysokości oraz minimalnej
/// i maksymalnej wartości wysokości na mapie.
/// </summary>

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
    {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);
        var heightCurveThreadsafe = new AnimationCurve(settings.heightCurve.keys);
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float value = values[i, j] * heightCurveThreadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;
                values[i, j] = value;
                if (value > maxValue) maxValue = value;
                if (value < minValue) minValue = value;
            }
        }

        return new HeightMap(values, minValue, maxValue);
    }
}

public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}