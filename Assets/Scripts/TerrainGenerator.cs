using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Klasa jest komponentem dla obiektu w Unity,
/// który generuje teren w czasie rzeczywistym na podstawie ustawień i parametrów.
/// Klasa ta zawiera zmienne prywatne i publiczne, takie jak m.in. indeksy LOD (poziomu szczegółowości),
/// ustawienia tekstury, ustawienia mapy wysokości, ustawienia siatki (mesh), obiekt widza,
/// czy materiał mapy, a także funkcje takie jak UpdateVisibleChunks()
/// oraz OnTerrainChunkVisibilityChanged(). Klasa ta odpowiada za generowanie terenu
/// i wizualizację go na ekranie w oparciu o aktualną pozycję widza.
/// </summary>

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private int colliderLODIndex;
    [SerializeField] private LODInfo[] detailLevels;
    [SerializeField] private MeshSettings meshSettings;
    [SerializeField] private HeightMapSettings heightMapSettings;
    [SerializeField] private TextureData textureSettings;
    [SerializeField] private Transform viewer;
    [SerializeField] private Material mapMaterial;

    private const float viewerMoveThresholdForChunkUpdate = 25f * 25f;
    private Vector2 m_ViewerPositionOld;
    private float m_MeshWorldSize;
    private int m_ChunksVisibleInViewDst;
    private readonly Dictionary<Vector2, TerrainChunk> m_TerrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private readonly List<TerrainChunk> m_VisibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        m_MeshWorldSize = meshSettings.MeshWorldSize;
        m_ChunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / m_MeshWorldSize);
        UpdateVisibleChunks();
    }

    private IEnumerator DelayedUpdateVisibleChunks()
    {
        yield return new WaitForEndOfFrame();
        UpdateVisibleChunks();
    }

    private void Update()
    {
        var position = viewer.position;
        var viewerPosition = new Vector2(position.x, position.z);
        if (!((m_ViewerPositionOld - viewerPosition).sqrMagnitude > viewerMoveThresholdForChunkUpdate)) return;
        m_ViewerPositionOld = viewerPosition;
        StartCoroutine(DelayedUpdateVisibleChunks());
    }

    private void UpdateVisibleChunks()
    {
        var position = viewer.position;
        var currentChunkCoord = new Vector2(Mathf.RoundToInt(position.x / m_MeshWorldSize), Mathf.RoundToInt(position.z / m_MeshWorldSize));
        var viewedChunkCoords = Enumerable.Range(-m_ChunksVisibleInViewDst, m_ChunksVisibleInViewDst * 2 + 1)
            .SelectMany(xOffset => Enumerable.Range(-m_ChunksVisibleInViewDst, m_ChunksVisibleInViewDst * 2 + 1)
                .Select(yOffset => new Vector2(currentChunkCoord.x + xOffset, currentChunkCoord.y + yOffset)));
        foreach (var viewedChunkCoord in viewedChunkCoords)
        {
            if (m_TerrainChunkDictionary.TryGetValue(viewedChunkCoord, out var chunk)) { chunk.UpdateTerrainChunk(); }
            else
            {
                var newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                m_TerrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                newChunk.Load();
            }
        }

        m_VisibleTerrainChunks
            .Where(chunk => (new Vector2(chunk.coord.x, chunk.coord.y) - currentChunkCoord).sqrMagnitude > m_ChunksVisibleInViewDst * m_ChunksVisibleInViewDst)
            .ToList()
            .ForEach(chunk => {
                chunk.SetVisible(false);
                m_VisibleTerrainChunks.Remove(chunk);
            });
    }


    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible) m_VisibleTerrainChunks.Add(chunk);
        else m_VisibleTerrainChunks.Remove(chunk);
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;

    public float visibleDstThreshold;
    public float SqrVisibleDstThreshold => visibleDstThreshold * visibleDstThreshold;
}