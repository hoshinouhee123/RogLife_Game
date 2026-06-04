using UnityEngine;

public class MeteorStar : MonoBehaviour
{
    private float fallSpeed;
    private Vector3 targetPos;
    private float damage;
    private GameObject fragmentPrefab;

    // ★ [새로 추가됨] 파편이 퍼져나가는 속도를 인스펙터에서 조절할 수 있게 합니다!
    [Header("파편 설정")]
    public float fragmentSpeed = 8f;

    // ★ [새로 추가됨] 내가 생성해 둔 바닥 경고 마커를 기억할 변수
    private GameObject myWarningMark;

    // 보스가 유성을 소환할 때 세팅값을 넘겨줍니다.
    public void Setup(float speed, Vector3 target, float dmg, GameObject fragment, GameObject warningPrefab)
    {
        fallSpeed = speed;
        targetPos = target;
        damage = dmg;
        fragmentPrefab = fragment;

        // ★ [핵심] 유성이 생성됨과 동시에, 바닥 목표 위치에 경고 마커를 띄워둡니다!
        if (warningPrefab != null)
        {
            myWarningMark = Instantiate(warningPrefab, targetPos, Quaternion.identity);
        }
    }

    void Update()
    {
        // 아래로 수직 낙하
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // 지정된 바닥 좌표(targetY)에 도달하면 폭발!
        if (transform.position.y <= targetPos.y)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.1f, 0.2f);

        if (fragmentPrefab != null)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;

                GameObject frag = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);

                // ★ [수정됨] Setup 함수의 마지막에 'fragmentSpeed'를 같이 넘겨줍니다!
                frag.GetComponent<EnemyBullet>().Setup(dir, damage, fragmentSpeed);
            }
        }

        if (myWarningMark != null) Destroy(myWarningMark);
        Destroy(gameObject);
    }
}