using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManagerPvP : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI[] buttonList;
    public TextMeshProUGUI playerXScoreText, playerOScoreText, playerXText, playerOText, drawText;
    public LineRenderer lineRenderer;
    public ParticleSystem particlePrefab;
    public GameObject redWinPanel, blueWinPanel;
    public Button redWinBackToMainMenuButton, redWinPlayAgainButton, blueWinBackToMainMenuButton, blueWinPlayAgainButton;
    public AudioSource drawLineAudioSource;

    private string playerSide, startingPlayerSide;
    private int moveCount, playerXScore, playerOScore;
    private Color playerColor;
    private ParticleSystem currentParticle;

    void Awake()
    {
        SetGameControllerReferenceOnButtons();
        startingPlayerSide = "X";
        playerSide = startingPlayerSide;
        playerColor = Color.red;
        moveCount = playerXScore = playerOScore = 0;
        ClearButtonTexts();
        UpdateScoreTexts();
        UpdateTextColors();

        lineRenderer.startWidth = lineRenderer.endWidth = 0.5f;
        lineRenderer.enabled = false;
        drawText.gameObject.SetActive(false);
        redWinPanel.SetActive(false);
        blueWinPanel.SetActive(false);

        redWinBackToMainMenuButton.onClick.AddListener(BackToMainMenu);
        redWinPlayAgainButton.onClick.AddListener(PlayAgain);
        blueWinBackToMainMenuButton.onClick.AddListener(BackToMainMenu);
        blueWinPlayAgainButton.onClick.AddListener(PlayAgain);
    }

    void SetGameControllerReferenceOnButtons()
    {
        foreach (var btn in buttonList)
            btn.GetComponentInParent<ButtonHandler>().SetGameControllerReference(this);
    }

    void ClearButtonTexts()
    {
        foreach (var btn in buttonList)
        {
            btn.text = "";
            btn.GetComponentInParent<ButtonHandler>().ResetButton();
        }
    }

    public string GetPlayerSide() => playerSide;
    public bool IsPlayerTurn() => true;

    public void EndTurn()
    {
        moveCount++;
        DecreaseLifetimes();
        if (CheckWin())
            GameOver();
        else if (moveCount >= 30)
            GameDraw();
        else
        {
            ChangeSides();
            UpdateTextColors();
        }
    }

    void DecreaseLifetimes()
    {
        foreach (var btn in buttonList)
            btn.GetComponentInParent<ButtonHandler>().DecreaseLifetime();
    }

    void ChangeSides()
    {
        if (playerSide == "X")
        {
            playerSide = "O";
            playerColor = Color.blue;
        }
        else
        {
            playerSide = "X";
            playerColor = Color.red;
        }
    }

    bool CheckWin()
    {
        int[][] lines = {
            new[] {0,1,2}, new[] {3,4,5}, new[] {6,7,8},
            new[] {0,3,6}, new[] {1,4,7}, new[] {2,5,8},
            new[] {0,4,8}, new[] {2,4,6}
        };
        foreach (var line in lines)
            if (CheckLine(line[0], line[1], line[2])) return true;
        return false;
    }

    bool CheckLine(int i1, int i2, int i3)
    {
        Color c1 = buttonList[i1].GetComponentInParent<ButtonHandler>().GetButtonColor();
        Color c2 = buttonList[i2].GetComponentInParent<ButtonHandler>().GetButtonColor();
        Color c3 = buttonList[i3].GetComponentInParent<ButtonHandler>().GetButtonColor();

        if (c1 == playerColor && c2 == playerColor && c3 == playerColor)
        {
            Color lineColor = (playerSide == "X") ? Color.red : Color.blue;
            Vector3 startPos = buttonList[i1].transform.position;
            Vector3 endPos = buttonList[i3].transform.position;
            Vector3 dir = (endPos - startPos).normalized;
            startPos -= dir * 1.2f;
            endPos += dir * 1.2f;
            StartCoroutine(AnimateLine(startPos, endPos, lineColor));
            return true;
        }
        return false;
    }

    IEnumerator AnimateLine(Vector3 start, Vector3 end, Color lineColor)
    {
        float duration = 1.8f, shrinkDuration = 1.0f, elapsed = 0f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, start);
        lineRenderer.startColor = lineRenderer.endColor = lineColor;
        lineRenderer.enabled = true;
        drawLineAudioSource.Play();

        currentParticle = Instantiate(particlePrefab, start, Quaternion.identity);
        var mainModule = currentParticle.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(lineColor);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 currentPos = Vector3.Lerp(start, end, elapsed / duration);
            lineRenderer.SetPosition(1, currentPos);
            currentParticle.transform.position = currentPos;
            yield return null;
        }

        drawLineAudioSource.Stop();
        lineRenderer.SetPosition(1, end);
        currentParticle.Stop();

        elapsed = 0f;
        Vector3 originalStart = lineRenderer.GetPosition(0), originalEnd = lineRenderer.GetPosition(1);
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            lineRenderer.SetPosition(0, Vector3.Lerp(originalStart, (originalStart + originalEnd) / 2, t));
            lineRenderer.SetPosition(1, Vector3.Lerp(originalEnd, (originalStart + originalEnd) / 2, t));
            yield return null;
        }

        lineRenderer.enabled = false;
        Destroy(currentParticle.gameObject, currentParticle.main.duration);
    }

    void GameOver()
    {
        SetButtonsInteractable(false);

        if (playerSide == "X")
        {
            playerXScore++;
            StartCoroutine(AnimateWinnerText(playerXText, Color.red));
        }
        else
        {
            playerOScore++;
            StartCoroutine(AnimateWinnerText(playerOText, Color.blue));
        }

        UpdateScoreTexts();

        if (playerXScore >= 3)
            ShowWinPanel("X");
        else if (playerOScore >= 3)
            ShowWinPanel("O");
        else
            Invoke(nameof(ResetGame), 2f);
    }

    void ShowWinPanel(string winner)
    {
        if (winner == "X") redWinPanel.SetActive(true);
        else blueWinPanel.SetActive(true);
    }

    void SetButtonsInteractable(bool interactable)
    {
        foreach (var btn in buttonList)
            btn.GetComponentInParent<Button>().interactable = interactable;
    }

    public void BackToMainMenu() => SceneManager.LoadScene(0);
    public void PlayAgain() => SceneManager.LoadScene(2);

    IEnumerator AnimateWinnerText(TextMeshProUGUI winnerText, Color particleColor)
    {
        float duration = 2f, rotationSpeed = 180f, elapsed = 0f;
        Vector3 originalScale = winnerText.transform.localScale, targetScale = originalScale * 4f;
        ParticleSystem winnerParticle = Instantiate(particlePrefab, winnerText.transform.position, Quaternion.identity);
        var mainModule = winnerParticle.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(particleColor);
        float particleLifetime = winnerParticle.main.duration + winnerParticle.main.startLifetime.constantMax;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            winnerText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            winnerText.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            winnerParticle.transform.position = winnerText.transform.position;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            winnerText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            winnerText.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            winnerParticle.transform.position = winnerText.transform.position;
            yield return null;
        }
        winnerText.transform.localScale = originalScale;
        winnerText.transform.rotation = Quaternion.identity;
        winnerParticle.Stop();
        yield return new WaitForSeconds(particleLifetime);
        Destroy(winnerParticle.gameObject);
    }

    void GameDraw()
    {
        SetButtonsInteractable(false);
        StartCoroutine(AnimateDrawText());
        StartCoroutine(AnimateDrawCentralText());
        Invoke(nameof(ResetGame), 2f);
    }

    IEnumerator AnimateDrawText()
    {
        float duration = 2f, elapsed = 0f;
        Vector3 scaleX = playerXText.transform.localScale, scaleO = playerOText.transform.localScale;
        Vector3 targetScale = scaleX * 2f;
        Vector3 fallDistance = new Vector3(0, -Screen.height / 2, 0);
        Vector3 posX = playerXText.transform.localPosition, posO = playerOText.transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerXText.transform.localScale = Vector3.Lerp(scaleX, targetScale, t);
            playerOText.transform.localScale = Vector3.Lerp(scaleO, targetScale, t);
            playerXText.transform.localPosition = Vector3.Lerp(posX, posX + fallDistance, t);
            playerOText.transform.localPosition = Vector3.Lerp(posO, posO + fallDistance, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerXText.transform.localScale = Vector3.Lerp(targetScale, scaleX, t);
            playerOText.transform.localScale = Vector3.Lerp(targetScale, scaleO, t);
            playerXText.transform.localPosition = Vector3.Lerp(posX + fallDistance, posX, t);
            playerOText.transform.localPosition = Vector3.Lerp(posO + fallDistance, posO, t);
            yield return null;
        }

        playerXText.transform.localScale = scaleX;
        playerXText.transform.localPosition = posX;
        playerOText.transform.localScale = scaleO;
        playerOText.transform.localPosition = posO;
    }

    IEnumerator AnimateDrawCentralText()
    {
        float duration = 2f, elapsed = 0f;
        Vector3 originalScale = drawText.transform.localScale, targetScale = originalScale * 4f;
        drawText.gameObject.SetActive(true);
        drawText.color = Color.yellow;
        drawText.text = "DRAW!";

        ParticleSystem drawParticle = Instantiate(particlePrefab, drawText.transform.position, Quaternion.identity);
        var mainModule = drawParticle.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(Color.yellow);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            drawText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            drawText.color = new Color(drawText.color.r, drawText.color.g, drawText.color.b, 1 - (elapsed / duration));
            drawText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }
        drawText.gameObject.SetActive(false);
        drawParticle.Stop();
        yield return new WaitForSeconds(drawParticle.main.duration);
        Destroy(drawParticle.gameObject);
    }

    void ResetGame()
    {
        ClearButtonTexts();
        lineRenderer.enabled = false;
        SwitchStartingPlayer();
        playerSide = startingPlayerSide;
        playerColor = (playerSide == "X") ? Color.red : Color.blue;
        UpdateTextColors();
        moveCount = 0;
        SetButtonsInteractable(true);
    }

    void SwitchStartingPlayer()
    {
        startingPlayerSide = (startingPlayerSide == "X") ? "O" : "X";
    }

    void UpdateScoreTexts()
    {
        playerXScoreText.text = playerXScore.ToString();
        playerOScoreText.text = playerOScore.ToString();
    }

    void UpdateTextColors()
    {
        playerXText.color = new Color(1, 0, 0, 0.5f);
        playerOText.color = new Color(0, 0, 1, 0.5f);
        playerXText.fontSize = 144;
        playerOText.fontSize = 144;

        if (playerSide == "X")
        {
            playerXText.color = new Color(1, 0, 0, 1);
            StartCoroutine(AnimateTextSize(playerXText, 144, 288));
            StartCoroutine(AnimateTextSize(playerOText, 288, 144));
        }
        else
        {
            playerOText.color = new Color(0, 0, 1, 1);
            StartCoroutine(AnimateTextSize(playerOText, 144, 288));
            StartCoroutine(AnimateTextSize(playerXText, 288, 144));
        }
    }

    IEnumerator AnimateTextSize(TextMeshProUGUI text, float fromSize, float toSize)
    {
        float duration = 0.5f, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.fontSize = Mathf.Lerp(fromSize, toSize, elapsed / duration);
            yield return null;
        }
        text.fontSize = toSize;
    }
}