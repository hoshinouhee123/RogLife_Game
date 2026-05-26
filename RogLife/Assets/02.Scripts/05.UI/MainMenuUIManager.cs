using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    public GameObject SettingPanel;

    private void Start()
    {
        Time.timeScale = 1f;
    }

    // ==========================================
    // ★ [추가됨] 시작 버튼용 징검다리 함수!
    // ==========================================
    public void ClickStartButton()
    {
        // 인스펙터 연결 없이, 코드로 '살아남은' SceneTransition 싱글톤을 찾아 명령을 내립니다!
        if (SceneTransition.instance != null)
        {
            SceneTransition.instance.LoadNextScene("PrologScene"); // 본 게임 씬 이름 적기
        }
        else
        {
            Debug.LogError("SceneTransition 매니저를 찾을 수 없습니다!");
        }
    }
    // ==========================================

    public void OpenSettingPanel()
    {
        if (SettingPanel != null) SettingPanel.SetActive(true);
    }

    public void CloseSettingPanel()
    {
        if (SettingPanel != null) SettingPanel.SetActive(false);
    }
}