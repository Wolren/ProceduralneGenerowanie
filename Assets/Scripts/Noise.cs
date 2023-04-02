using System.Linq;
using UnityEngine;

/// <summary>
/// Klasa zawiera funkcję GenerateNoiseMap,
/// która generuje szum Perlin o określonych wymiarach i na podstawie ustawień szumu.
/// Funkcja przyjmuje cztery argumenty: szerokość mapy, wysokość mapy, ustawienia szumu oraz punkt próbki.
/// 
/// Klasa NoiseSettings to serializowalna klasa, która zawiera ustawienia szumu.
/// Zawiera ona właściwości normalizacja, skala, oktawy, stałość, lakunarność oraz ziarno.
/// Metoda ValidateValues służy do upewnienia się, że wartości ustawień są poprawne.
///
/// Funkcja GenerateNoiseMap generuje mapę szumu o podanych wymiarach i na podstawie ustawień szumu.
/// Funkcja zaczyna od wygenerowania ziarna za pomocą klasy System.Random.
/// Następnie generowane są offsety oktaw. Maksymalna możliwa wysokość mapy jest obliczana na podstawie ustawień szumu,
/// a następnie obliczana jest najwyższa i najniższa wartość szumu w celu normalizacji mapy.
/// 
/// Następnie funkcja przechodzi przez każdy piksel na mapie i wylicza dla niego wartość szumu.
/// Dla każdego piksela obliczana jest suma wartości szumu dla każdej oktawy.
/// Wartość szumu dla każdej oktawy jest obliczana za pomocą szumu Perlin,
/// a następnie mnożona przez stałość i częstotliwość. Wartości te są sumowane,
/// a ostateczna wartość szumu jest zapisywana w tablicy.
///
/// Jeśli normalizacja jest ustawiona na TAK,
/// mapa jest normalizowana przez podzielenie każdej wartości szumu przez maksymalną możliwą wysokość mapy,
/// a następnie skalowanie wartości w przedziale od 0 do 0.9.
/// W przeciwnym razie, wartości są skalowane tak,
/// aby najniższa wartość szumu była równa 0, a najwyższa równa 1.
/// </summary>

public static class Noise
{
    public enum NormalizeMode
    {
        TAK,
        NIE
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        var prng = new System.Random(settings.ziarno);
        var octaveOffsets = Enumerable.Range(0, settings.oktawy)
            .Select(_ => new Vector2(prng.Next(-100000, 100000) + sampleCentre.x,
                prng.Next(-100000, 100000) - sampleCentre.y))
            .ToArray();
        float maxPossibleHeight = Enumerable.Range(0, settings.oktawy)
            .Select(i =>
            {
                float amp = Mathf.Pow(settings.stałość, i);
                return amp;
            })
            .Sum();
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = Enumerable.Range(0, settings.oktawy)
                    .Select(i =>
                    {
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.skala * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.skala * frequency;
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        amplitude *= settings.stałość;
                        frequency *= settings.lakunarność;
                        return perlinValue * amplitude;
                    })
                    .Sum();

                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;
                noiseMap[x, y] = noiseHeight;
                if (settings.normalizacja == NormalizeMode.TAK)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        if (settings.normalizacja == NormalizeMode.NIE)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings
{
    public Noise.NormalizeMode normalizacja;
    public float skala = 50;
    public int oktawy = 6;
    [Range(0, 1)] public float stałość = .6f;
    public float lakunarność = 2;
    public int ziarno;

    public void ValidateValues()
    {
        skala = Mathf.Max(skala, 0.01f);
        oktawy = Mathf.Max(oktawy, 1);
        lakunarność = Mathf.Max(lakunarność, 1);
        stałość = Mathf.Clamp01(stałość);
    }
}