using UnityEngine;
using System.Collections;

public class LaserBlaster : MonoBehaviour
{
    [Header("레이저 부품 연결")]
    public SpriteRenderer moonRenderer;
    public SpriteRenderer warningRenderer;
    public SpriteRenderer laserRenderer;
    public BoxCollider2D laserCollider;

    [Header("타이밍 설정")]
    public float warningTime = 0.8f;
    public float fireDuration = 0.5f;

    private float damage = 1f;

    private void Start()
    {
        // ★ [핵심 방어막] 코루틴이 에러로 멈추더라도, 3초 뒤엔 무조건 찌꺼기 없이 파괴됩니다!
        Destroy(gameObject, warningTime + fireDuration + 0.5f);
    }

    public void Setup(float dmg)
    {
        damage = dmg;
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        // 1. 초기화 (두 번째 쏠 때 투명해지는 버그 완벽 방지)
        laserCollider.enabled = false;
        laserRenderer.gameObject.SetActive(false);
        moonRenderer.color = Color.white;

        warningRenderer.gameObject.SetActive(true);
        warningRenderer.color = new Color(1f, 0f, 0f, 0.4f); // 반투명 빨강

        // 2. 경고 대기
        yield return new WaitForSeconds(warningTime);

        // 3. 발사!
        if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.2f, 0.3f);

        warningRenderer.gameObject.SetActive(false); // 경고선 끄기
        laserRenderer.gameObject.SetActive(true);    // 진짜 레이저 켜기
        laserRenderer.color = Color.white;           // 색상 100%
        laserCollider.enabled = true;                // 피격 판정 켜기

        // 4. 레이저 유지
        yield return new WaitForSeconds(fireDuration);

        // 5. 스르륵 사라지기
        laserCollider.enabled = false;
        float fadeTimer = 0f;
        while (fadeTimer < 0.2f)
        {
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / 0.2f);
            moonRenderer.color = new Color(1f, 1f, 1f, alpha);
            laserRenderer.color = new Color(1f, 1f, 1f, alpha); // 레이저도 서서히 투명
            yield return null;
        }

        // 투명해지면 즉시 파괴
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 트리거에 머무는 동안 플레이어에게 데미지
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Player>().TakeDamage(Mathf.RoundToInt(damage));
        }
    }
}