using TMPro;
using UnityEngine;

public class AuthUI : MonoBehaviour
{
    [SerializeField] private AuthenticationManagerWithPassword auth;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text statusText;

    public async void OnRegisterClicked()
    {
        statusText.text = "Registering...";
        string msg = await auth.RegisterWithUsernameAndPassword(usernameInput.text, passwordInput.text);
        statusText.text = msg;
    }

    public async void OnLoginClicked()
    {
        statusText.text = "Logging in...";
        string msg = await auth.LoginWithUsernameAndPassword(usernameInput.text, passwordInput.text);
        statusText.text = msg;
    }
}
