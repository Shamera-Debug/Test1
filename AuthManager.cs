using System;
using Firebase.Auth;
using Firebase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public TMP_Text feedbackText;

    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        loginButton.onClick.AddListener(Login);
        registerButton.onClick.AddListener(Register);
    }

    void Login()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                feedbackText.text = "Login failed: The operation was canceled.";
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                HandleAuthException(task.Exception);
                return;
            }

            FirebaseUser user = auth.CurrentUser;
            if (user != null)
            {
                Debug.LogFormat("User signed in successfully: {0} ({1})", user.DisplayName, user.UserId);
                feedbackText.text = "Login successful!";
                
                GameManager.Instance.InitializeFirebase(); // Firebase 초기화

                SceneManager.LoadScene("MainMenuScene");
            }
            else
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync succeeded but CurrentUser is null.");
                feedbackText.text = "Login failed: User information is missing.";
            }
        });
    }

    void Register()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                feedbackText.text = "Registration failed: The operation was canceled.";
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                HandleAuthException(task.Exception);
                return;
            }

            FirebaseUser newUser = auth.CurrentUser;
            if (newUser != null)
            {
                Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                feedbackText.text = "Registration successful!";
                SceneManager.LoadScene("MainMenuScene");
            }
            else
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync succeeded but CurrentUser is null.");
                feedbackText.text = "Registration failed: User information is missing.";
            }
        });
    }

    void HandleAuthException(AggregateException exception)
    {
        if (exception.InnerExceptions.Count > 0)
        {
            FirebaseException firebaseEx = exception.InnerExceptions[0] as FirebaseException;
            if (firebaseEx != null)
            {
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                string message;
                switch (errorCode)
                {
                    case AuthError.AccountExistsWithDifferentCredentials:
                        message = "An account already exists with different credentials.";
                        break;
                    case AuthError.MissingPassword:
                        message = "Password is missing.";
                        break;
                    case AuthError.WeakPassword:
                        message = "The password is too weak.";
                        break;
                    case AuthError.WrongPassword:
                        message = "The password is incorrect.";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "An account already exists with this email.";
                        break;
                    case AuthError.InvalidEmail:
                        message = "The email address is badly formatted.";
                        break;
                    case AuthError.MissingEmail:
                        message = "Email is missing.";
                        break;
                    default:
                        message = "An internal error has occurred.";
                        break;
                }
                feedbackText.text = "Failed: " + message;
                Debug.LogError("Firebase Auth Error: " + message + "\nException: " + exception);
            }
            else
            {
                feedbackText.text = "Failed: An unknown error occurred.";
                Debug.LogError("Unknown Auth Error: " + exception.InnerExceptions[0].Message);
            }
        }
    }
}
