using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManagerWithPassword : MonoBehaviour
{
    [SerializeField] private GameObject authPanel; // גררי את הפאנל מה-Inspector

    private async void Awake()
    {
        Debug.Log("AuthenticationManagerWithPassword Awake");

        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        Debug.Log("UGS Initialized. SignedIn? " + AuthenticationService.Instance.IsSignedIn);

        // אם כבר מחוברת – תכבי ישר
        if (AuthenticationService.Instance.IsSignedIn)
            HideAuthPanel();
        else
            ShowAuthPanel();
    }

    public async Task<string> RegisterWithUsernameAndPassword(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            HideAuthPanel(); // <-- כאן
            return $"Register successful! Player ID: {AuthenticationService.Instance.PlayerId}";
        }
        catch (AuthenticationException ex) { return $"Register failed: {ex.Message}"; }
        catch (RequestFailedException ex) { return $"Register request failed: {ex.Message}"; }
    }

    public async Task<string> LoginWithUsernameAndPassword(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            HideAuthPanel(); // <-- כאן
            return $"Login successful! Player ID: {AuthenticationService.Instance.PlayerId}";
        }
        catch (AuthenticationException ex) { return $"Login failed: {ex.Message}"; }
        catch (RequestFailedException ex) { return $"Login request failed: {ex.Message}"; }
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        Debug.Log("Player signed out");
        ShowAuthPanel(); // אם התנתקת – להחזיר את הפאנל
    }

    private void HideAuthPanel()
    {
        if (authPanel != null) authPanel.SetActive(false);
    }

    private void ShowAuthPanel()
    {
        if (authPanel != null) authPanel.SetActive(true);
    }
}
