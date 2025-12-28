using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;

public static class CloudSaveSystem
{
    public static async Task SaveStringAsync(string key, string value)
    {
        var payload = new Dictionary<string, object> { { key, value } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(payload);
    }

    public static async Task<string> LoadStringAsync(string key)
    {
        var keys = new HashSet<string> { key };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (result == null || !result.TryGetValue(key, out Item item))
            return null;

        return item.Value.GetAs<string>();
    }

    public static async Task DeleteAsync(string key)
    {
        // Use the new DeleteOptions from Unity.Services.CloudSave.Models.Data.Player
        await CloudSaveService.Instance.Data.Player.DeleteAsync(
            key, 
            new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions()
        );
    }
}
