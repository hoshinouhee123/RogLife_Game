using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    // ★ [추가됨] 현재 흔들리는 중인지 확인하고 관리할 코루틴 변수
    private Coroutine shakeCoroutine;
    private Vector3 originalPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        // ★ [핵심 해결 코드] 이미 흔들리고 있는 중이라면?
        if (shakeCoroutine != null)
        {
            // 흔들림 연출만 초기화하고, 원래 위치(originalPos)는 새로 덮어쓰지 않습니다!
            StopCoroutine(shakeCoroutine);
        }
        else
        {
            // 흔들리지 않고 가만히 있을 때만 '현재 위치'를 정답으로 저장합니다!
            originalPos = transform.position;
        }

        // 새로운 흔들림 시작
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
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

        // 진동이 끝나면 기억해둔 완벽한 원래 위치로 정확히 복구
        transform.position = originalPos;

        // ★ 흔들림이 완전히 끝났음을 알림 (다음 진동을 위해 비워둠)
        shakeCoroutine = null;
    }
}