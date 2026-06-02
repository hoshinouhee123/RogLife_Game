using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    private Player player;
    private SpriteMask spriteMask;

    void Start()
    {
        player = GetComponentInParent<Player>();
        spriteMask = GetComponent<SpriteMask>();

        // ★ [수정됨] 이제 껐다 켰다 하지 않고, 항상 마스크(빛)를 켜둡니다!
        if (spriteMask != null) spriteMask.enabled = true;
    }

    void Update()
    {
        if (player == null) return;

        // ★ [수정됨] 공격 중이 아니더라도, 플레이어가 '마지막으로 바라본 방향(lastFacingDir)'으로 계속 빛을 비춥니다!
        float angle = Mathf.Atan2(player.lastFacingDir.y, player.lastFacingDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}