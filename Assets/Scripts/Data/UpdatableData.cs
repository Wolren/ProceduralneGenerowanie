using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValuesUpdated;

#if UNITY_EDITOR
    public void NotifyOfUpdatedValues()
    {
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
        OnValuesUpdated?.Invoke();
    }
#endif
}