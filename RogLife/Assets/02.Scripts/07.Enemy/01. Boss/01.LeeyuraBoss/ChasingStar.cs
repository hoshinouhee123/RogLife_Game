using UnityEngine;
using System.Collections;

public class ChasingStar : MonoBehaviour
{
    private Transform player;
    private float damage;
    private bool isActive = true;
    private SpriteRenderer sr;

    public void Setup(Transform targetPlayer, float dmg)
    {
        player = targetPlayer;
        damage = dmg;
        sr = GetComponent<SpriteRenderer>();

        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        // 1. 처음엔 투명한 상태에서 스르륵 나타납니다
        Color c = sr.color;
        c.a = 0f;
        sr.color = c;

        while (c.a < 1f)
        {
            c.a += Time.deltaTime * 2f;
            sr.color = c;
            yield return null;
        }

        // 2. [대기 -> 부드러운 이동 -> 대기] 반복!
        while (isActive)
        {
            // 가만히 멈춰서 노려보는 시간 (0.6초)
            yield return new WaitForSeconds(0.6f);

            if (!isActive || player == null) break;

            // 이동할 목표 위치 계산 (플레이어 쪽으로 3.5칸)
            Vector3 startPos = transform.position;
            Vector3 dir = (player.position - transform.position).normalized;
            Vector3 targetPos = transform.position + dir * 3.5f;

            // ★ [여기 수정됨] 이동하는 시간(1.0초)을 늘려서 훅! 들어오지 않게 합니다.
            float timer = 0f;
            float moveTime = 1.0f;

            while (timer < moveTime && isActive)
            {
                timer += Time.deltaTime;
                float t = timer / moveTime;

                // ★ [핵심] 서서히 출발 -> 스무스하게 이동 -> 서서히 멈춤 (SmoothStep 공식)
                float ease = t * t * (3f - 2f * t);

                transform.position = Vector3.Lerp(startPos, targetPos, ease);
                yield return null;
            }
        }
    }

    // 보스 패턴이 끝나면 자연스럽게 사라지라고 명령받는 함수
    public void FadeOutAndDestroy()
    {
        isActive = false;
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        // 스르륵 투명해진 뒤 삭제
        Color c = sr.color;
        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * 3f;
            sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }

    // 플레이어와 부딪히면 데미지!
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isActive && collision.CompareTag("Player"))
        {
            collision.GetComponent<Player>().TakeDamage(Mathf.RoundToInt(damage));
        }
    }
}