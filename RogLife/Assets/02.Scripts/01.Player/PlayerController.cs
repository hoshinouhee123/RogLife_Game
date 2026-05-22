using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;    // 플레이어 이동 속도

    public Sprite[] spriteUp;   // 위쪽 이동 애니메이션 스프라이트 배열
    public Sprite[] spriteDown; // 아래쪽 이동 애니메이션 스프라이트 배열
    public Sprite[] spriteLeft; // 왼쪽 이동 애니메이션 스프라이트 배열
    public Sprite[] spriteRight; // 오른쪽 이동 애니메이션 스프라이트 배열

    public float frameTime = 0.15f; // 애니메이션 프레임 간격

    private Rigidbody2D rb;     // 플레이어의 Rigidbody2D 컴포넌트

    private SpriteRenderer sr; // 플레이어의 SpriteRenderer 컴포넌트

    private Vector2 input;   // 현재 입력 방향

    private Vector2 velocity;  // 현재 이동 속도

    private Sprite[] currentSprites; // 현재 애니메이션 스프라이트 배열

    private int frameIndex = 0; // 현재 애니메이션 프레임 인덱스

    private float timer = 0f; // 애니메이션 타이머

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        currentSprites = spriteDown; // 초기 애니메이션은 아래쪽 이동으로 설정
        sr.sprite = currentSprites[0];  // 초기 스프라이트 설정
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>();
        velocity = input.normalized * moveSpeed;

        if (input.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                if (input.x > 0)
                    ChangeSprites(spriteRight);
                else
                    ChangeSprites(spriteLeft);
            }
            else
            {
                if (input.y > 0)
                    ChangeSprites(spriteUp);
                else
                    ChangeSprites(spriteDown);
            }
        }
    }

    private void Update()
    {
        if (input.sqrMagnitude <= 0.01f)
        {
            frameIndex = 0;
            sr.sprite = currentSprites[frameIndex];
            return;
        }

        timer += Time.deltaTime;

        if (timer >= frameTime)
        {
            timer = 0f;
            frameIndex++;

            if (frameIndex >= currentSprites.Length)
                frameIndex = 0;

            sr.sprite = currentSprites[frameIndex];

        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    private void ChangeSprites(Sprite[] newSprites)
    {
        if (currentSprites == newSprites)
            return;

        currentSprites = newSprites;
        frameIndex = 0;
        timer = 0f;
        sr.sprite = currentSprites[frameIndex];
    }
}


