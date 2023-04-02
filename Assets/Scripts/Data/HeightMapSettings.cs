using UnityEngine;

[CreateAssetMenu]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    public float heightMultiplier;
    public AnimationCurve heightCurve;
    public float MinHeight => heightMultiplier * heightCurve.Evaluate(0);
    public float MaxHeight => heightMultiplier * heightCurve.Evaluate(1);
}