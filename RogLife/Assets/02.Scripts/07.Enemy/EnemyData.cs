using UnityEngine;

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
}