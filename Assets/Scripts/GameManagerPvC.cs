using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManagerPvC : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI[] buttonList;
    public TextMeshProUGUI playerScoreText, computerScoreText, playerText, computerText, drawText;
    public LineRenderer lineRenderer;
    public ParticleSystem particlePrefab;
    public GameObject redWinPanel, blueWinPanel;
    public Button redWinBackToMainMenuButton, redWinPlayAgainButton, blueWinBackToMainMenuButton, blueWinPlayAgainButton;
    public AudioSource drawLineAudioSource;

    private string startingPlayerSide;
    private int moveCount, playerScore, computerScore;
    private Color playerColor = Color.red, computerColor = Color.blue;
    private string computerSide = "O";
    private bool isPlayerTurn;
    private ParticleSystem currentParticle;

    void Awake()
    {
        SetGameControllerReferenceOnButtons();
        startingPlayerSide = "X";
        isPlayerTurn = true;
        moveCount = playerScore = computerScore = 0;
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

        if (!isPlayerTurn)
            Invoke(nameof(ComputerMove), 2f);
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

    public string GetPlayerSide() => "X";
    public bool IsPlayerTurn() => isPlayerTurn;

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
            isPlayerTurn = !isPlayerTurn;
            UpdateTextColors();
            if (!isPlayerTurn)
                Invoke(nameof(ComputerMove), 1.5f);
        }
    }

    void DecreaseLifetimes()
    {
        foreach (var btn in buttonList)
            btn.GetComponentInParent<ButtonHandler>().DecreaseLifetime();
    }

    void GameDraw()
    {
        SetButtonsInteractable(false);
        StartCoroutine(AnimateDrawText());
        StartCoroutine(AnimateDrawCentralText());
        Invoke(nameof(ResetGame), 2f);
    }

    void SetButtonsInteractable(bool interactable)
    {
        foreach (var btn in buttonList)
            btn.GetComponentInParent<Button>().interactable = interactable;
    }

    IEnumerator AnimateDrawText()
    {
        float duration = 2f;
        Vector3 scaleX = playerText.transform.localScale, scaleO = computerText.transform.localScale;
        Vector3 targetScale = scaleX * 2f;
        Vector3 fallDistance = new Vector3(0, -Screen.height / 2, 0);
        Vector3 posX = playerText.transform.localPosition, posO = computerText.transform.localPosition;

        yield return AnimateScaleAndPosition(playerText, scaleX, targetScale, posX, posX + fallDistance, duration);
        yield return AnimateScaleAndPosition(computerText, scaleO, targetScale, posO, posO + fallDistance, duration);
        yield return AnimateScaleAndPosition(playerText, targetScale, scaleX, posX + fallDistance, posX, duration);
        yield return AnimateScaleAndPosition(computerText, targetScale, scaleO, posO + fallDistance, posO, duration);

        playerText.transform.localScale = scaleX;
        playerText.transform.localPosition = posX;
        computerText.transform.localScale = scaleO;
        computerText.transform.localPosition = posO;
    }

    IEnumerator AnimateScaleAndPosition(TextMeshProUGUI text, Vector3 fromScale, Vector3 toScale, Vector3 fromPos, Vector3 toPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            text.transform.localScale = Vector3.Lerp(fromScale, toScale, t);
            text.transform.localPosition = Vector3.Lerp(fromPos, toPos, t);
            yield return null;
        }
    }

    IEnumerator AnimateDrawCentralText()
    {
        float duration = 2f;
        Vector3 originalScale = drawText.transform.localScale, targetScale = originalScale * 4f;
        drawText.gameObject.SetActive(true);
        drawText.color = Color.yellow;
        drawText.text = "DRAW!";

        ParticleSystem drawParticle = Instantiate(particlePrefab, drawText.transform.position, Quaternion.identity);
        var mainModule = drawParticle.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(Color.yellow);
        yield return AnimateTextScale(drawText, originalScale, targetScale, duration);
        yield return AnimateTextFade(drawText, targetScale, originalScale, duration);

        drawText.gameObject.SetActive(false);
        drawParticle.Stop();
        Destroy(drawParticle.gameObject, drawParticle.main.duration);
    }

    IEnumerator AnimateTextScale(TextMeshProUGUI text, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            text.transform.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
    }

    IEnumerator AnimateTextFade(TextMeshProUGUI text, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1 - t);
            text.transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
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

        if ((c1 == playerColor && c2 == playerColor && c3 == playerColor) ||
            (c1 == computerColor && c2 == computerColor && c3 == computerColor))
        {
            Color lineColor = (c1 == playerColor) ? playerColor : computerColor;
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
        var currentMain = currentParticle.main;
        currentMain.startColor = new ParticleSystem.MinMaxGradient(lineColor);

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

        if (isPlayerTurn)
        {
            playerScore++;
            StartCoroutine(AnimateWinnerText(playerText, playerColor));
        }
        else
        {
            computerScore++;
            StartCoroutine(AnimateWinnerText(computerText, computerColor));
        }

        UpdateScoreTexts();

        if (playerScore >= 3)
            ShowWinPanel("X");
        else if (computerScore >= 3)
            ShowWinPanel("O");
        else
            Invoke(nameof(ResetGame), 2f);
    }

    void ShowWinPanel(string winner)
    {
        if (winner == "X") redWinPanel.SetActive(true);
        else blueWinPanel.SetActive(true);
    }

    public void BackToMainMenu() => SceneManager.LoadScene(0);
    public void PlayAgain() => SceneManager.LoadScene(1);

    IEnumerator AnimateWinnerText(TextMeshProUGUI winnerText, Color particleColor)
    {
        float duration = 2f, rotationSpeed = 180f, elapsed = 0f;
        Vector3 originalScale = winnerText.transform.localScale, targetScale = originalScale * 4f;
        ParticleSystem winnerParticle = Instantiate(particlePrefab, winnerText.transform.position, Quaternion.identity);
        var winnerMain = winnerParticle.main;
        winnerMain.startColor = new ParticleSystem.MinMaxGradient(particleColor);
        float particleLifetime = winnerParticle.main.duration + winnerParticle.main.startLifetime.constantMax;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            winnerText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            winnerText.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            winnerParticle.transform.position = winnerText.transform.position;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            winnerText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
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

    void ResetGame()
    {
        ClearButtonTexts();
        lineRenderer.enabled = false;
        SwitchStartingPlayer();
        isPlayerTurn = startingPlayerSide == "X";
        UpdateTextColors();
        moveCount = 0;
        SetButtonsInteractable(true);

        if (!isPlayerTurn)
            Invoke(nameof(ComputerMove), 1f);
    }

    void SwitchStartingPlayer()
    {
        startingPlayerSide = (startingPlayerSide == "X") ? "O" : "X";
    }

    void UpdateScoreTexts()
    {
        playerScoreText.text = playerScore.ToString();
        computerScoreText.text = computerScore.ToString();
    }

    void UpdateTextColors()
    {
        playerText.color = new Color(1, 0, 0, isPlayerTurn ? 1 : 0.5f);
        computerText.color = new Color(0, 0, 1, isPlayerTurn ? 0.5f : 1);
        playerText.fontSize = isPlayerTurn ? 288 : 144;
        computerText.fontSize = isPlayerTurn ? 144 : 288;
        StartCoroutine(AnimateTextSize(playerText, playerText.fontSize, isPlayerTurn ? 288 : 144));
        StartCoroutine(AnimateTextSize(computerText, computerText.fontSize, isPlayerTurn ? 144 : 288));
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

    // --- Computer AI ---
    void ComputerMove()
    {
        if (TryToWinOrBlock(computerColor, 1)) return;
        if (TryToWinOrBlock(playerColor, 2)) return;
        if (PlaceLongestLivedComputerButton()) return;

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < buttonList.Length; i++)
            if (buttonList[i].text == "")
                availableIndices.Add(i);

        if (availableIndices.Count > 0)
        {
            int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            buttonList[randomIndex].GetComponentInParent<ButtonHandler>().SetSpaceForComputer(computerSide, computerColor);
        }
        EndTurn();
    }

    bool TryToWinOrBlock(Color color, int priority)
    {
        for (int i = 0; i < buttonList.Length; i += 3)
            if (CheckTwoInLine(color, i, i + 1, i + 2, priority)) return true;
        for (int i = 0; i < 3; i++)
            if (CheckTwoInLine(color, i, i + 3, i + 6, priority)) return true;
        if (CheckTwoInLine(color, 0, 4, 8, priority)) return true;
        if (CheckTwoInLine(color, 2, 4, 6, priority)) return true;
        return false;
    }

    bool CheckTwoInLine(Color color, int i1, int i2, int i3, int priority)
    {
        var h1 = buttonList[i1].GetComponentInParent<ButtonHandler>();
        var h2 = buttonList[i2].GetComponentInParent<ButtonHandler>();
        var h3 = buttonList[i3].GetComponentInParent<ButtonHandler>();

        if (h1.GetButtonColor() == color && h2.GetButtonColor() == color && buttonList[i3].text == "")
        {
            if (priority == 1 && (h1.GetLifetime() == 1 || h2.GetLifetime() == 1)) return false;
            if (priority == 2 && (h1.GetLifetime() == 2 || h2.GetLifetime() == 2)) return false;
            h3.SetSpaceForComputer(computerSide, computerColor); EndTurn(); return true;
        }
        if (h1.GetButtonColor() == color && h3.GetButtonColor() == color && buttonList[i2].text == "")
        {
            if (priority == 1 && (h1.GetLifetime() == 1 || h3.GetLifetime() == 1)) return false;
            if (priority == 2 && (h1.GetLifetime() == 2 || h3.GetLifetime() == 2)) return false;
            h2.SetSpaceForComputer(computerSide, computerColor); EndTurn(); return true;
        }
        if (h2.GetButtonColor() == color && h3.GetButtonColor() == color && buttonList[i1].text == "")
        {
            if (priority == 1 && (h2.GetLifetime() == 1 || h3.GetLifetime() == 1)) return false;
            if (priority == 2 && (h2.GetLifetime() == 2 || h3.GetLifetime() == 2)) return false;
            h1.SetSpaceForComputer(computerSide, computerColor); EndTurn(); return true;
        }
        return false;
    }

    bool PlaceLongestLivedComputerButton()
    {
        int maxLifetime = -1, bestIndex = -1;
        for (int i = 0; i < buttonList.Length; i++)
        {
            var handler = buttonList[i].GetComponentInParent<ButtonHandler>();
            if (handler.GetButtonColor() == computerColor && handler.GetLifetime() > maxLifetime)
            {
                maxLifetime = handler.GetLifetime();
                bestIndex = i;
            }
        }
        if (bestIndex != -1)
        {
            foreach (int index in GetAdjacentIndices(bestIndex))
            {
                if (buttonList[index].text == "")
                {
                    buttonList[index].GetComponentInParent<ButtonHandler>().SetSpaceForComputer(computerSide, computerColor);
                    EndTurn();
                    return true;
                }
            }
        }
        return false;
    }

    int[] GetAdjacentIndices(int index)
    {
        switch (index)
        {
            case 0: return new[] { 1, 2, 3, 6, 4, 8 };
            case 1: return new[] { 0, 2, 4, 7 };
            case 2: return new[] { 0, 1, 5, 8, 4, 6 };
            case 3: return new[] { 0, 6, 4, 5 };
            case 4: return new[] { 0, 8, 2, 6, 1, 7, 3, 5 };
            case 5: return new[] { 2, 8, 3, 4 };
            case 6: return new[] { 0, 3, 7, 8, 2, 4 };
            case 7: return new[] { 1, 4, 6, 8 };
            case 8: return new[] { 0, 4, 2, 6, 5, 7 };
            default: return new int[0];
        }
    }
}