using System;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManagerWithPassword : MonoBehaviour
{
    [SerializeField] private GameObject authPanel;

    public static bool UgsReady { get; private set; } = false;
    public static bool AnalyticsReady { get; private set; } = false;

    private static bool _initStarted = false;

    private async void Awake()
    {
        Debug.Log("[UGS] AuthenticationManagerWithPassword Awake");

        // כדי שלא יאתחל פעמיים אם יש את זה בכמה סצנות
        if (_initStarted)
        {
            UpdatePanel();
            return;
        }

        _initStarted = true;
        DontDestroyOnLoad(gameObject);

        await InitUgsAndAnalytics();

        UpdatePanel();
    }

    private async Task InitUgsAndAnalytics()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            UgsReady = true;
            Debug.Log("[UGS] Initialized");

            // ✅ חשוב: Analytics
            try
            {
                AnalyticsService.Instance.StartDataCollection();
                AnalyticsReady = true;
                Debug.Log("[UGS] Analytics StartDataCollection OK");
            }
            catch (Exception e)
            {
                AnalyticsReady = false;
                Debug.LogError("[UGS] Analytics StartDataCollection FAILED: " + e);
            }
        }
        catch (Exception e)
        {
            UgsReady = false;
            AnalyticsReady = false;
            Debug.LogError("[UGS] InitializeAsync FAILED: " + e);
        }
    }

    private void UpdatePanel()
    {
        Debug.Log("[UGS] SignedIn? " + AuthenticationService.Instance.IsSignedIn);

        if (AuthenticationService.Instance.IsSignedIn) HideAuthPanel();
        else ShowAuthPanel();
    }

    public async Task<string> RegisterWithUsernameAndPassword(string username, string password)
    {
        if (!UgsReady)
            await InitUgsAndAnalytics();

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            HideAuthPanel();
            return $"Register successful! Player ID: {AuthenticationService.Instance.PlayerId}";
        }
        catch (AuthenticationException ex) { return $"Register failed: {ex.Message}"; }
        catch (RequestFailedException ex) { return $"Register request failed: {ex.Message}"; }
        catch (Exception ex) { return $"Register unexpected error: {ex}"; }
    }

    public async Task<string> LoginWithUsernameAndPassword(string username, string password)
    {
        if (!UgsReady)
            await InitUgsAndAnalytics();

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            HideAuthPanel();
            return $"Login successful! Player ID: {AuthenticationService.Instance.PlayerId}";
        }
        catch (AuthenticationException ex) { return $"Login failed: {ex.Message}"; }
        catch (RequestFailedException ex) { return $"Login request failed: {ex.Message}"; }
        catch (Exception ex) { return $"Login unexpected error: {ex}"; }
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        Debug.Log("[UGS] Player signed out");
        ShowAuthPanel();
    }

    private void HideAuthPanel()
    {
        if (authPanel != null) authPanel.SetActive(false);
    }
    public async void GuestLogin()
    {
        
        if (!UgsReady)
            await InitUgsAndAnalytics();

       
        const string guestUser = "gal";
        const string guestPass = "123456789Ga=l";

        
        string result = await LoginWithUsernameAndPassword(guestUser, guestPass);
        Debug.Log("[UGS] GuestLogin: " + result);

    }


    private void ShowAuthPanel()
    {
        if (authPanel != null) authPanel.SetActive(true);
    }
}
