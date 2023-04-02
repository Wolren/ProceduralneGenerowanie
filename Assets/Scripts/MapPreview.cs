using UnityEngine;
/// <summary>
/// Klasa służy do wyświetlania podglądu mapy terenu w edytorze.
/// 
/// Metoda DrawTexture(Texture texture) służy do rysowania tekstury na podstawie podanego obiektu Texture,
/// natomiast metoda DrawMesh(MeshData meshData) służy do rysowania siatki terenu na podstawie podanego obiektu MeshData.
/// Metoda OnTextureValuesUpdated() jest wywoływana po aktualizacji wartości związanych z teksturą.
/// 
/// Klasa posiada także metodę DrawMapInEditor(), która aktualizuje ustawienia siatki oraz tekstury,
/// a następnie rysuje mapę w edytorze na podstawie wybranej przez użytkownika metody rysowania,
/// która może być wybrana z enuma DrawMode.
///
/// Klasa MapPreview także rejestruje zdarzenia OnValuesUpdated() i OnTextureValuesUpdated()
/// w metodzie OnValidate(). W zależności od ustawień klasy, te metody są wywoływane po zmianie ustawień siatki,
/// ustawień mapy wysokości i ustawień tekstury,
/// a także w momencie gdy wartości są aktualizowane podczas działania aplikacji.
/// </summary>

public class MapPreview : MonoBehaviour
{
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public DrawMode drawMode;
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public Material terrainMaterial;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;

    public enum DrawMode
    {
        NOISE_MAP,
        MESH
    }

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        var heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, heightMapSettings, Vector2.zero);
        switch (drawMode)
        {
            case DrawMode.NOISE_MAP:
                DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
                break;
            case DrawMode.MESH:
                DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
                break;
        }
    }

    private void DrawTexture(Texture texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
        textureRender.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    private void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying) DrawMapInEditor();
    }

    private void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}