using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Klasa jest używana do generowania i zarządzania fragmentami terenu w grach.
/// Klasa ta posiada pola przechowujące ustawienia terenu oraz detale poziomu szczegółowości (LOD),
/// a także metody do generowania wysokości mapy, aktualizowania fragmentów terenu oraz kolizji.
/// 
/// Pole coord przechowuje współrzędne fragmentu terenu,
/// a m_SampleCentre określa środek fragmentu w przestrzeni trójwymiarowej.
/// Pole m_MaxViewDst określa maksymalną odległość, na jakiej fragment terenu jest widoczny,
/// a m_DetailLevels przechowuje ustawienia poziomów szczegółowości LOD dla fragmentu terenu.
/// 
/// Metoda Load() wywołuje asynchroniczną metodę HeightMapGenerator.GenerateHeightMap(),
/// która generuje wysokości mapy dla fragmentu terenu. Kiedy wysokości mapa zostanie wygenerowana,
/// metoda OnHeightMapReceived() jest wywoływana z zwróconą mapą wysokości, która jest przechowywana w polu m_HeightMap.
///
/// Metoda UpdateTerrainChunk() jest wywoływana co klatkę
/// i aktualizuje widoczność fragmentu terenu oraz poziom szczegółowości,
/// w zależności od pozycji kamery w grze. Jeśli fragment terenu jest widoczny,
/// LOD jest aktualizowany i aktualna siatka jest wyświetlana na ekranie.
///
/// Metoda UpdateCollisionMesh() generuje kolizje fragmentu terenu, gdy gracz zbliża się do niego.
/// Jeśli odległość między graczem a fragmentem terenu jest mniejsza niż ustalona wartość, generowany jest collider dla fragmentu terenu.
///
/// Metoda SetVisible() ustawia widoczność fragmentu terenu, a IsVisible() zwraca informację o tym, czy dany fragment terenu jest widoczny.
///
/// Wewnątrz klasy znajduje się także wewnętrzna klasa LODMesh, która przechowuje siatkę dla danego poziomu szczegółowości LOD.
/// </summary>

public class TerrainChunk
{
    private const float COLLIDER_GENERATION_DISTANCE_THRESHOLD = 5;
    public event Action<TerrainChunk, bool> OnVisibilityChanged;
    public Vector2 coord;
    private readonly GameObject m_MeshObject;
    private readonly Vector2 m_SampleCentre;
    private Bounds m_Bounds;
    private readonly MeshFilter m_MeshFilter;
    private readonly MeshCollider m_MeshCollider;
    private readonly LODInfo[] m_DetailLevels;
    private readonly LODMesh[] m_LODMeshes;
    private readonly int m_ColliderLODIndex;
    private HeightMap m_HeightMap;
    private bool m_HeightMapReceived;
    private int m_PreviousLODIndex = -1;
    private bool m_HasSetCollider;
    private readonly float m_MaxViewDst;
    private readonly HeightMapSettings m_HeightMapSettings;
    private readonly MeshSettings m_MeshSettings;
    private readonly Transform m_Viewer;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.coord = coord;
        m_DetailLevels = detailLevels;
        m_ColliderLODIndex = colliderLODIndex;
        m_HeightMapSettings = heightMapSettings;
        m_MeshSettings = meshSettings;
        m_Viewer = viewer;
        m_SampleCentre = coord * meshSettings.MeshWorldSize / meshSettings.skalaSiatki;
        var position = coord * meshSettings.MeshWorldSize;
        m_Bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);
        m_MeshObject = new GameObject("Chunk")
        {
            transform =
            {
                position = new Vector3(position.x, 0, position.y),
                parent = parent
            }
        };
        SetVisible(false);
        m_MeshObject.AddComponent<MeshRenderer>().material = material;
        m_MeshFilter = m_MeshObject.AddComponent<MeshFilter>();
        m_MeshCollider = m_MeshObject.AddComponent<MeshCollider>();
        m_LODMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            m_LODMeshes[i] = new LODMesh(detailLevels[i].lod);
            m_LODMeshes[i].UpdateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex) m_LODMeshes[i].UpdateCallback += UpdateCollisionMesh;
        }

        m_MaxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(m_MeshSettings.NumVertsPerLine, m_MeshSettings.NumVertsPerLine, m_HeightMapSettings, m_SampleCentre), OnHeightMapReceived);
    }

    private void OnHeightMapReceived(object heightMapObject)
    {
        m_HeightMap = (HeightMap)heightMapObject;
        m_HeightMapReceived = true;
        UpdateTerrainChunk();
    }

    private Vector2 ViewerPosition => new Vector2(m_Viewer.position.x, m_Viewer.position.z);

    public void UpdateTerrainChunk()
    {
        if (!m_HeightMapReceived) return;
    
        float viewerDstFromNearestEdge = Mathf.Sqrt(m_Bounds.SqrDistance(ViewerPosition));
        bool wasVisible = IsVisible();
        bool visible = viewerDstFromNearestEdge <= m_MaxViewDst;
    
        if (visible)
        {
            int lodIndex = Enumerable.Range(0, m_DetailLevels.Length - 1).FirstOrDefault(i => viewerDstFromNearestEdge <= m_DetailLevels[i].visibleDstThreshold);
            if (lodIndex != m_PreviousLODIndex)
            {
                var lodMesh = m_LODMeshes[lodIndex];
                if (lodMesh.HasMesh)
                {
                    m_PreviousLODIndex = lodIndex;
                    m_MeshFilter.mesh = lodMesh.Mesh;
                }
                else if (!lodMesh.HasRequestedMesh) lodMesh.RequestMesh(m_HeightMap, m_MeshSettings);
            }
        }
    
        if (wasVisible == visible) return;
        SetVisible(visible);
        OnVisibilityChanged?.Invoke(this, visible);
    }

    private void UpdateCollisionMesh()
    {
        if (m_HasSetCollider) return;

        var viewerPosition = ViewerPosition;
        var sqrDstFromViewerToEdge = m_Bounds.SqrDistance(viewerPosition);
        var threshold = m_DetailLevels[m_ColliderLODIndex].SqrVisibleDstThreshold;

        if (sqrDstFromViewerToEdge >= threshold) return;

        var lodMesh = m_LODMeshes[m_ColliderLODIndex];
        if (!lodMesh.HasRequestedMesh)
        {
            lodMesh.RequestMesh(m_HeightMap, m_MeshSettings);
        }

        const float COLLIDER_THRESHOLD = COLLIDER_GENERATION_DISTANCE_THRESHOLD * COLLIDER_GENERATION_DISTANCE_THRESHOLD;
        if (sqrDstFromViewerToEdge >= COLLIDER_THRESHOLD || !lodMesh.HasMesh) return;

        m_MeshCollider.sharedMesh = lodMesh.Mesh;
        m_HasSetCollider = true;
    }

    public void SetVisible(bool visible)
    {
        m_MeshObject.SetActive(visible);
    }

    private bool IsVisible() => m_MeshObject.activeSelf;

    private class LODMesh
    {
        private readonly int m_LOD;

        public LODMesh(int lod)
        {
            m_LOD = lod;
        }

        public Mesh Mesh { get; private set; }
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh => Mesh != null;
        public event Action UpdateCallback;

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            HasRequestedMesh = true;

            void OnMeshDataReceived(object meshDataObject)
            {
                Mesh = ((MeshData)meshDataObject).CreateMesh();
                UpdateCallback?.Invoke();
            }

            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, m_LOD), OnMeshDataReceived);
        }
    }
}