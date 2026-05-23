using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    public GameObject SettingPanel;


    

    // Update is called once per frame
    void Update()
    {
        
    }


    public void OpenSettingPanel()
    {
        if (SettingPanel != null)
        {
            SettingPanel.SetActive(true);
        }
    }

    public void CloseSettingPanel()
    {
        if (SettingPanel != null)
        {
            SettingPanel.SetActive(false);
        }
    }
}
