using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 화면을 X, Y축으로 랜덤하게 덜덜 떨게 만듭니다.
            float x = originalPos.x + Random.Range(-1f, 1f) * magnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(x, y, originalPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 진동이 끝나면 원래 위치로 정확히 복구
        transform.position = originalPos;
    }
}