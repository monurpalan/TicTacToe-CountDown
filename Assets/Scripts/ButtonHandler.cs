using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ButtonHandler : MonoBehaviour
{
    [Header("UI References")]
    public Button button;
    public TextMeshProUGUI buttonText;
    public Image buttonImage;
    public AudioSource audioSource;

    private GameManagerPvC gameControllerPvC;
    private GameManagerPvP gameControllerPvP;
    private int lifetime;

    public void SetGameControllerReference(GameManagerPvC controller) => gameControllerPvC = controller;
    public void SetGameControllerReference(GameManagerPvP controller) => gameControllerPvP = controller;

    public void SetSpace()
    {
        if (!IsButtonAvailable()) return;

        StartCoroutine(AnimateButtonPress());

        if (gameControllerPvC != null)
            SetSpaceForPvC();
        else if (gameControllerPvP != null)
            SetSpaceForPvP();
    }

    private bool IsButtonAvailable()
    {
        bool isEmpty = buttonText != null && buttonText.text == "";
        bool isPlayerTurn = (gameControllerPvC != null && gameControllerPvC.IsPlayerTurn()) ||
                            (gameControllerPvP != null && gameControllerPvP.IsPlayerTurn());
        return isEmpty && isPlayerTurn;
    }

    private void SetSpaceForPvC()
    {
        SetButtonState(gameControllerPvC.GetPlayerSide(), Color.red);
        gameControllerPvC.EndTurn();
    }

    private void SetSpaceForPvP()
    {
        string playerSide = gameControllerPvP.GetPlayerSide();
        Color sideColor = (playerSide == "X") ? Color.red : Color.blue;
        SetButtonState(playerSide, sideColor);
        gameControllerPvP.EndTurn();
    }

    public void SetSpaceForComputer(string side, Color color)
    {
        if (buttonText != null && buttonText.text == "")
        {
            StartCoroutine(AnimateButtonPress());
            SetButtonState(side, color);
        }
    }

    private void SetButtonState(string text, Color color)
    {
        buttonText.text = text;
        button.interactable = false;
        buttonImage.color = color;
        lifetime = 7;
    }

    public void DecreaseLifetime()
    {
        if (lifetime <= 0) return;

        lifetime--;
        buttonText.text = lifetime > 0 ? lifetime.ToString() : "";
        if (lifetime == 0)
            ClearButton();
    }

    private void ClearButton()
    {
        buttonText.text = "";
        buttonImage.color = Color.white;
        button.interactable = true;
    }

    public Color GetButtonColor() => buttonImage.color;
    public int GetLifetime() => lifetime;

    public void ResetButton()
    {
        buttonText.text = "";
        buttonImage.color = Color.white;
        button.interactable = true;
        lifetime = 0;
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private IEnumerator AnimateButtonPress()
    {
        Vector3 originalScale = button.transform.localScale;
        Vector3 pressedScale = originalScale * 0.9f;
        float duration = 0.1f;

        yield return AnimateScale(originalScale, pressedScale, duration);
        PlayButtonSound();
        yield return AnimateScale(pressedScale, originalScale, duration);
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            button.transform.localScale = Vector3.Lerp(from, to, elapsedTime / duration);
            yield return null;
        }
    }
}