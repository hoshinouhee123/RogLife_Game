using UnityEngine;

public class StarFalling : MonoBehaviour
{
    public float fallSpeed = 20f; // 떨어지는 속도

    private Vector3 targetPos;
    private float damage;
    private GameObject laserPrefab;

    // 몬스터 AI가 별을 소환할 때 목표 위치와 레이저 프리팹을 알려줍니다.
    public void Setup(Vector3 target, float dmg, GameObject laser)
    {
        targetPos = target;
        damage = dmg;
        laserPrefab = laser;

        // ★ 타겟 위치보다 오른쪽 위(대각선 15, 15 거리)로 강제 순간이동 시킨 뒤 떨어지게 합니다!
        transform.position = targetPos + new Vector3(15f, 15f, 0);
    }

    void Update()
    {
        // 목표 위치(바닥)를 향해 대각선으로 미친듯이 떨어짐
        transform.position = Vector3.MoveTowards(transform.position, targetPos, fallSpeed * Time.deltaTime);

        // 바닥에 거의 다 닿았다면?
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            ExplodeCrossLaser();
        }
    }

    void ExplodeCrossLaser()
    {
        if (laserPrefab != null)
        {
            // ★ 기존에 만든 초승달 레이저를 4방향으로 회전시켜서 소환 = 십자 레이저 완성!
            SpawnLaser(0f);    // 오른쪽
            SpawnLaser(90f);   // 위쪽
            SpawnLaser(180f);  // 왼쪽
            SpawnLaser(-90f);  // 아래쪽

            if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.15f, 0.3f);
        }

        Destroy(gameObject); // 별은 터지고 사라짐
    }

    void SpawnLaser(float angle)
    {
        GameObject laser = Instantiate(laserPrefab, targetPos, Quaternion.Euler(0, 0, angle));
        laser.GetComponent<LaserBlaster>().Setup(damage);
    }
}