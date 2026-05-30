using UnityEngine;
// UnityEngine.InputSystem은 더 이상 필요 없으므로 제거했습니다.

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;    // 플레이어 이동 속도

    public Sprite[] spriteUp;   // 위쪽 이동 애니메이션 스프라이트 배열
    public Sprite[] spriteDown; // 아래쪽 이동 애니메이션 스프라이트 배열
    public Sprite[] spriteLeft; // 왼쪽 이동 애니메이션 스프라이트 배열
    public Sprite[] spriteRight; // 오른쪽 이동 애니메이션 스프라이트 배열

    public float frameTime = 0.15f; // 애니메이션 프레임 간격

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector2 input;   // 현재 입력 방향
    private Vector2 velocity;  // 현재 이동 속도

    private Sprite[] currentSprites; // 현재 애니메이션 스프라이트 배열
    private int frameIndex = 0;
    private float timer = 0f;

    // 콜라이더 범위 안에 들어온 대상을 임시로 기억해둘 변수
    private InteractableObject currentInteractable = null;

    public bool isHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        currentSprites = spriteDown; // 초기 애니메이션은 아래쪽 이동으로 설정
        sr.sprite = currentSprites[0];  // 초기 스프라이트 설정
    }

    private void Update()
    {
        // ★ [새로 추가됨] 치트 콘솔이 켜져있으면 이동, 상호작용 등 모든 키보드 입력을 무시합니다!
        if (CheatConsole.Instance != null && CheatConsole.Instance.isConsoleActive) return;

        // 1. 대화 중일 때의 플레이어 제어
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive)
        {
            input = Vector2.zero;
            velocity = Vector2.zero;

            frameIndex = 0;
            sr.sprite = currentSprites[0];

            
            return;
        }

        // 2. 평상시 상호작용 (대화 시작)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            if (currentInteractable != null)
            {
                input = Vector2.zero;
                velocity = Vector2.zero;
                currentInteractable.Interact();
                return;
            }
        }

        // ==========================================
        // 3. ★오직 WASD 키만 사용하여 이동 방향 정하기★
        // ==========================================
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY += 1f; // W 누르면 위로
        if (Input.GetKey(KeyCode.S)) moveY -= 1f; // S 누르면 아래로
        if (Input.GetKey(KeyCode.D)) moveX += 1f; // D 누르면 오른쪽으로
        if (Input.GetKey(KeyCode.A)) moveX -= 1f; // A 누르면 왼쪽으로

        // 대각선 이동 시 속도가 빨라지는 것을 막기 위해 normalized 사용
        input = new Vector2(moveX, moveY).normalized;
        velocity = input * moveSpeed;

        // ==========================================
        // 4. 입력 방향에 따른 애니메이션(스프라이트) 변경
        // ==========================================
        if (!isHit) // ★ 추가됨: 피격 중이 아닐 때만 걷는 애니메이션 재생!
        {
            if (input.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                {
                    if (input.x > 0) ChangeSprites(spriteRight);
                    else ChangeSprites(spriteLeft);
                }
                else
                {
                    if (input.y > 0) ChangeSprites(spriteUp);
                    else ChangeSprites(spriteDown);
                }

                timer += Time.deltaTime;
                if (timer >= frameTime)
                {
                    timer = 0f;
                    frameIndex++;
                    if (frameIndex >= currentSprites.Length) frameIndex = 0;
                    sr.sprite = currentSprites[frameIndex];
                }
            }
            else
            {
                frameIndex = 0;
                sr.sprite = currentSprites[frameIndex];
            }
        }
    }

    private void FixedUpdate()
    {
        if (DialogueManager.instance != null && DialogueManager.instance.isDialogueActive) return;

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    private void ChangeSprites(Sprite[] newSprites)
    {
        if (currentSprites == newSprites) return;

        currentSprites = newSprites;
        frameIndex = 0;
        timer = 0f;
        sr.sprite = currentSprites[frameIndex];
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        InteractableObject interactable = collision.GetComponent<InteractableObject>();
        if (interactable != null)
        {
            currentInteractable = interactable;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<InteractableObject>() != null)
        {
            currentInteractable = null;
        }
    }
}