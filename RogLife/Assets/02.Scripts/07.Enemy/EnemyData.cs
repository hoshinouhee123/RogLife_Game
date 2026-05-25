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

    // 몬스터 전용 효과음
    [Header("효과음")]
    public AudioClip hitSound;    // 맞았을 때 소리
    public AudioClip deathSound;  // 죽을 때  소리

    // 이 몬스터가 보스일 경우 출력될 대화문!
    // (일반 몬스터일 때는 그냥 비워두면 됩니다)
    [Header("보스 전용 대화")]
    public DialogueLine[] bossDialogues;

    // 몬스터 효과음용 믹서 그룹
    public AudioMixerGroup sfxMixerGroup;
}