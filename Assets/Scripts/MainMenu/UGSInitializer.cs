using UnityEngine;
using Unity.Services.Core;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    public static bool Ready { get; private set; }

    private async void Awake()
    {
        await UnityServices.InitializeAsync();
        Ready = true;
        Debug.Log("UGS Initialized");
    }
}
