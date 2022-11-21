using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnterGame : MonoBehaviour
{
    public GameObject SignInView;

    public Button EnterButton;

    public TMP_InputField UserNameInput;
    bool m_CanEnter;
    private void Awake()
    {

        FramworkEvent.LanguageInit.AddEventHandler(ShowSignInView);

        EnterButton?.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(UserNameInput.text))
                UserNameInput.text = "test";
            if (!string.IsNullOrEmpty(UserNameInput.text))
            {
                NetAdapter.Instance.Login(UserNameInput.text, () =>
                {

                    GameInit.Instance.EnterGame();
                    EnterButton.gameObject.SetActive(false);
                    UserNameInput.gameObject.SetActive(false);
                }
                );
            }
        }

        );
        NetWorkBehavior.Instance.OnConnected += () =>
        {
            m_CanEnter = true;
        };
    }
    public void Update()
    {
        if (m_CanEnter)
        {
            EnterButton.gameObject.SetActive(true);
            m_CanEnter = false;
        }
    }
    void OnDestroy()
    {
        FramworkEvent.LanguageInit.RemoveEventHandler(ShowSignInView);
    }

    void ShowSignInView()
    {
        SignInView.gameObject.SetActive(true);
    }
}
