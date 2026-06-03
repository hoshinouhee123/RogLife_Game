using UnityEngine;
using UnityEngine.Audio;

// 유니티 프로젝트 창에서 우클릭으로 이 데이터를 바로 생성할 수 있게 해주는 마법의 코드
[CreateAssetMenu(fileName = "New Enemy Data", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;      // 몬스터 이름
    public Sprite enemySprite;    // 몬스터 이미지 (도트)

    [Header("전투 스탯")]
    public float maxHealth;       // 최대 체력
    public float moveSpeed;       // 이동 속도
    public float damage;          // 플레이어에게 부딪혔을 때 주는 피해량

    [Header("원거리 공격 몹 설정")]
    public bool isShooter = false;           // 체크하면 원거리 몹이 됨
    public GameObject enemyBulletPrefab;     // 날릴 투사체 프리팹
    public float fireRate = 1.5f;            // 공격 속도 (1.5초마다 1발)
    public float attackRange = 8f;           // 이 사거리 안에 들어오면 쏘기 시작함

    // ★ [새로 추가됨] 투사체 발사 효과음!
    public AudioClip shootSound;

    [Header("사망 시 분열 몹 설정")]
    public bool isNormalSplitter = false;    // 체크하면 죽을 때 2마리로 분열함

    // 몬스터 전용 효과음
    [Header("효과음")]
    public AudioClip hitSound;    // 맞았을 때 소리
    public AudioClip deathSound;  // 죽을 때  소리

    // 이 몬스터가 보스일 경우 출력될 대화문!
    // (일반 몬스터일 때는 그냥 비워두면 됩니다)
    [Header("보스 전용 대화")]
    public DialogueLine[] bossDialogues;

    // [새로 추가된 부분] 보스전 전용 BGM!
    [Header("보스 BGM (일반 몹은 비워두세요)")]
    public AudioClip bossBgm;

    // 몬스터 효과음용 믹서 그룹
    public AudioMixerGroup sfxMixerGroup;

    [Header("보스 특수 기믹 (분열 & 대쉬)")]
    public bool isDashSplittingBoss = false; // 체크하면 대쉬/분열 보스가 됨!
    public float dashSpeedMultiplier = 4f;   // 평소 속도보다 대쉬할 때 몇 배 빠른가?
    public float dashPrepTime = 1.0f;        // 대쉬하기 전 기 모으는 시간 (초)
    public float stunTime = 2.0f;            // 벽에 부딪혔을 때 기절하는 시간 (초)
    public float wallCrashDamage = 5f;       // 벽에 박았을 때 입는 자해 데미지

    // ★ [새로 추가] 죽을 때 나올 이펙트 프리팹
    public GameObject deathEffectPrefab;

    // ==========================================
    // ★ [새로 추가됨] 적 개별 충돌 판정(콜라이더) 설정
    // ==========================================
    [Header("충돌 판정 크기 설정")]
    [Tooltip("체크하면 아래의 크기로 콜라이더가 조절됩니다.")]
    public bool useCustomHitbox = false;
    public float hitboxRadius = 0.5f;              // 원형 콜라이더의 반지름
    public Vector2 hitboxOffset = new Vector2(0f, 0f); // 중심점 위치

    // ==========================================
    // ★ [새로 추가됨] 2층 보스: 은신 & 시야 기믹
    // ==========================================
    [Header("보스 특수 기믹 (은신 & 시야)")]
    public bool isStealthBoss = false;       // 체크하면 은신 보스가 됩니다.
    public float sightAngle = 45f;           // 플레이어의 시야각 (45도면 총 90도 부채꼴

    // [기존 코드 아래에 추가]
    [Header("최종 보스 설정 (5층)")]
    public bool isFinalBoss = false;          // 체크하면 5층 보스가 됨!
    public GameObject laserBlasterPrefab;     // 방금 만든 초승달 레이저 프리팹
    public float patternCooldown = 4.0f;      // 평타 추격 후 레이저 패턴을 쓰는 주기

    [Header("보스 전용 설정")]
    public float bossScale = 2f; // ★ 일반 보스는 2, 5층 보스는 1로 설정하세요!

    [Header("이동 애니메이션 (선택)")]
    // 이 배열에 이미지를 채워 넣으면 걸을 때 자동으로 바뀝니다! (비워두면 안 바뀜)
    public Sprite[] animUp;
    public Sprite[] animDown;
    public Sprite[] animLeft;
    public Sprite[] animRight;
    public float animFrameTime = 0.15f; // 프레임 속도
}