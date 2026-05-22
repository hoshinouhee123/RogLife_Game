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

    // 콜라이더 범위 안에 들어온 대상을 임시로 기억해둘 변수
    private InteractableObject currentInteractable = null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        currentSprites = spriteDown; // 초기 애니메이션은 아래쪽 이동으로 설정
        sr.sprite = currentSprites[0];  // 초기 스프라이트 설정
    }

    public void OnMove(InputValue value)
    {
        // 대화 중이면 키보드 이동 입력을 무시합니다.
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive) return;

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
        // 1. 대화 중일 때의 플레이어 제어
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
        {
            // 이동 속도와 입력을 0으로 강제 초기화하여 정지 상태로 만듬.
            input = Vector2.zero;
            velocity = Vector2.zero;

            // 걷는 모션을 멈추고 서있는 모션으로 고정.
            frameIndex = 0;
            sr.sprite = currentSprites[0];

            // 대화 넘기기 키 입력만 허용.
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                DialogueManager.instance.DisplayNextSentence();
            }

            // 대화 중일 땐 아래의 이동 애니메이션이나 새로운 상호작용 코드가 실행되지 않게함
            return;
        }

        // 2. 평상시 상호작용 (대화 시작)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            if (currentInteractable != null)
            {
                // 말을 거는 순간 미끄러지지 않게 즉시 멈춤 처리
                input = Vector2.zero;
                velocity = Vector2.zero;

                currentInteractable.Interact();
                return; // 대화가 시작되면 Update()의 나머지 이동 애니메이션 로직이 실행되지 않도록 return으로 빠져나감
            }
        }

        // 3. 기존 애니메이션 로직 (대화 중이 아닐 때만 실행됨)
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
        // 대화 중이면 물리적 이동을 완전히 차단.
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive) return;

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

    // 플레이어가 NPC의 BoxCollider2D(Trigger) 안으로 들어왔을 때 실행됨
    private void OnTriggerEnter2D(Collider2D collision)
    {
        InteractableObject interactable = collision.GetComponent<InteractableObject>();

        // 부딪힌 대상이 상호작용 가능한 오브젝트라면 기억해둠
        if (interactable != null)
        {
            currentInteractable = interactable;
        }
    }

    // 플레이어가 NPC의 BoxCollider2D(Trigger) 밖으로 나갔을 때 실행됨
    private void OnTriggerExit2D(Collider2D collision)
    {
        // 범위를 벗어나면 기억을 지움 (멀어지면 말 못 걸게)
        if (collision.GetComponent<InteractableObject>() != null)
        {
            currentInteractable = null;
        }
    }
}


