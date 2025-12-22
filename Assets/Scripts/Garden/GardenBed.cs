using UnityEngine;

public class GardenBed : MonoBehaviour
{
    public bool HasCrop { get; private set; }
    public bool IsMature { get; private set; }
    public bool Reserved { get; private set; }

    public void PlantSeed() { HasCrop = true; IsMature = false; Reserved = false; }
    public void SetMature() { if (HasCrop) IsMature = true; }
    public bool TryReserve()
    {
        if (!HasCrop || !IsMature || Reserved) return false;
        Reserved = true;
        return true;
    }
    public void ClearCrop() { HasCrop = false; IsMature = false; Reserved = false; }
}
