using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void LoadPvPScene() => SceneManager.LoadScene(2);

    public void LoadPvCScene() => SceneManager.LoadScene(1);
}