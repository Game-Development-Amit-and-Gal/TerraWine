using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GardenManager : MonoBehaviour
{
    [SerializeField] private List<GardenBed> beds;

    public bool TryGetRandomStealable(out GardenBed bed)
    {
        var options = beds.Where(b => b != null && b.HasCrop && b.IsMature && !b.Reserved).ToList();
        if (options.Count == 0) { bed = null; return false; }

        bed = options[Random.Range(0, options.Count)];
        return bed.TryReserve(); // reserve it
    }
}
