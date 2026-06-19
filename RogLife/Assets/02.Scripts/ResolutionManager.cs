using UnityEngine;

public class ResolutionManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SetFixedResolution()
    {
        int width = 1920;
        int height = 1080;


        Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);

        Debug.Log($"해상도가 {width}x{height}로 강제 고정되었습니다!");
    }
}