using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class MeshSettings : UpdatableData
{
    public const int numSupportedLODs = 5;
    private const int numSupportedChunkSizes = 9;
    private const int numSupportedFlatshadedChunkSizes = 3;
    private static readonly int[] _supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
    public float skalaSiatki = 2.5f;
    public bool użyjCieniowania;
    [FormerlySerializedAs("RozmiarChunka")] [FormerlySerializedAs("IndeksRozmiaruChunka")] [Range(0, numSupportedChunkSizes - 1)] public int rozmiarChunka;

    [FormerlySerializedAs("RozmiarZacienionegoChunka")] [Range(0, numSupportedFlatshadedChunkSizes - 1)]
    public int rozmiarZacienionegoChunka;

    public int NumVertsPerLine => _supportedChunkSizes[(użyjCieniowania) ? rozmiarZacienionegoChunka : rozmiarChunka] + 5;
    public float MeshWorldSize => (NumVertsPerLine - 3) * skalaSiatki;
}