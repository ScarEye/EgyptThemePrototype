using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController instance;

    public GameObject enemies;
    public ThirdPersonController thirdPersonController;
    public TextMeshProUGUI endGameTxt;
    public GameObject endGamePanel;

    // Start is called before the first frame update
    void Start()
    {
        if (instance==null)
        {
            instance = this;
        }

        Invoke("StartGame", 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void StartGame()
    {
        for (int i=0;i<enemies.transform.childCount;i++)
        {
            enemies.transform.GetChild(i).gameObject.SetActive(true);
        }
        thirdPersonController.SetPlayerInput(true);
    }
    public void WinnerPanel()
    {
        thirdPersonController.SetPlayerInput(false);
        EndGamePanel(true, "Congratulations!\nYou Escaped from the ruins.");
    }

    public void LosePanel()
    {
        thirdPersonController.PlayerDead();
        EndGamePanel(true,"Game Over!\nRestart to try again.");
    }

    void EndGamePanel(bool canActive,string txt)
    {
        endGameTxt.text = txt;
        endGamePanel.SetActive(canActive);
    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }
}
