using UnityEngine;
using System.Collections;

public class PhantomDash : MonoBehaviour
{
    public float trackTime = 1.0f; // 경고 대기 시간
    public float dashSpeed = 40f;

    private float damage;
    private bool isDashing = false;

    // (기존의 player와 startX 변수는 더 이상 추적하지 않으므로 사용하지 않습니다)

    public void Setup(Transform targetPlayer, float dmg, Sprite phantomSprite, float spawnX)
    {
        damage = dmg;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (phantomSprite != null) sr.sprite = phantomSprite;
        sr.color = new Color(1f, 0.3f, 0.3f, 0.8f);

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        // 1. 1초 동안 제자리에서 대기 (더 이상 위아래로 쫓아다니지 않음!)
        float timer = 0f;
        while (timer < trackTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. 대기 완료! 돌진 시작
        isDashing = true;
    }

    private void Update()
    {
        // 왼쪽으로 일직선 돌진!
        if (isDashing)
        {
            transform.Translate(Vector3.left * dashSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Player>().TakeDamage(Mathf.RoundToInt(damage));
        }
        else if (isDashing && (collision.CompareTag("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("EnemyBlocker")))
        {
            if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.2f, 0.4f);
            Destroy(gameObject);
        }
    }
}