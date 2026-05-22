using UnityEngine;

[System.Serializable] // 인스펙터 창에 노출되도록 설정
public struct DialogueLine
{
    public Sprite characterPortrait; // 캐릭터 일러스트
    [TextArea(3, 5)]
    public string sentence;          // 대화 내용
}