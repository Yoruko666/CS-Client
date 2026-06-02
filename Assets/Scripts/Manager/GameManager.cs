using UnityEngine;

public class GameManager : SingletonMono<GameManager>
{
    [HideInInspector] public bool isMainScene;

    void Start()
    {
        isMainScene = true;
    }

    void Update()
    {
        
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
