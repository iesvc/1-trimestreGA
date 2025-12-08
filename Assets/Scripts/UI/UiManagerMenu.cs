using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiManagerMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject volumePanel;
    public GameObject exitConfirmPanel;
    
    public void VolumenPanel()
    {
        AudioManager.Instance.PlaySFX("Boton");
        mainPanel.SetActive(false);
        volumePanel.SetActive(true);
    }

    public void ExitPanel()
    {
        AudioManager.Instance.PlaySFX("Boton");
        mainPanel.SetActive(false);
        exitConfirmPanel.SetActive(true);
    }

    public void BotonNoExit()
    {
        AudioManager.Instance.PlaySFX("Boton");
        mainPanel.SetActive(true);
        exitConfirmPanel.SetActive(false);
    }

    public void BotonCancel()
    {
        AudioManager.Instance.PlaySFX("Boton");
        mainPanel.SetActive(true);
        volumePanel.SetActive(false);
    }

    public void BotonPlay()
    {
        AudioManager.Instance.PlaySFX("Boton");
        SceneManager.LoadScene("EscenaPrincipal");
    }

    public void BotonTutorial()
    {
        AudioManager.Instance.PlaySFX("Boton");
        Invoke("ChangeSceneTutorial",0.4f);
    }

    public void BotonExit()
    {
        AudioManager.Instance.PlaySFX("Boton");
        Application.Quit();
    }

    public void BotonMenu()
    {
        AudioManager.Instance.PlaySFX("Boton");
        Invoke("ChangeSceneMenu",0.4f);
    }


    void ChangeSceneMenu()
    {
        SceneManager.LoadScene("MenuPrincipal"); 
    }

    void ChangeSceneTutorial()
    {
        SceneManager.LoadScene("Tutorial"); 
    }
}