using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CheatConsole : MonoBehaviour
{
    public static CheatConsole Instance;

    [Header("UI 연결")]
    public RectTransform consolePanel; // 콘솔창 전체 패널
    public TMP_InputField inputField;      // 명령어 치는 곳
    public TextMeshProUGUI logText;               // 결과가 뜰 텍스트 창

    // ★ [새로 추가됨] 스크롤 뷰를 조작하기 위한 변수
    public UnityEngine.UI.ScrollRect logScrollRect;

    // ★ [수정된 코드] 다른 곳에서 확인할 수 있게 public으로 바꿉니다!
    public bool isConsoleActive = false;
    private float slideDuration = 0.2f;

    // [변수 선언부에 추가]
    [Header("스폰용 프리팹 (치트)")]
    public GameObject spawnCoinPrefab;
    public GameObject spawnKeyPrefab;
    public GameObject spawnHeartPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        // 시작 시 화면 위쪽 바깥(-Y)이 아니라 상단 위쪽(+Y)으로 숨겨둡니다.
        consolePanel.anchoredPosition = new Vector2(0, consolePanel.rect.height);
        consolePanel.gameObject.SetActive(false);

        // 엔터를 쳤을 때 함수 실행하도록 연결
        inputField.onEndEdit.AddListener(OnSubmitCommand);
    }

    private void Update()
    {
        // ` 키 (물결표 키) 를 누르면 콘솔 켜기/끄기
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleConsole();
        }
    }

    private void ToggleConsole()
    {
        isConsoleActive = !isConsoleActive;
        StopAllCoroutines();
        StartCoroutine(SlideConsole(isConsoleActive));

        if (isConsoleActive)
        {
            Time.timeScale = 0f; // 게임 정지
            consolePanel.gameObject.SetActive(true);
            inputField.text = "";
            inputField.ActivateInputField(); // 켜지자마자 바로 타자 칠 수 있게 포커스
        }
        else
        {
            Time.timeScale = 1f; // 게임 재개
        }
    }

    private IEnumerator SlideConsole(bool show)
    {
        float targetY = show ? 0f : consolePanel.rect.height;
        float startY = consolePanel.anchoredPosition.y;
        float timer = 0f;

        while (timer < slideDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / slideDuration;
            // 부드럽게 위아래로 미끄러지는 연출
            float ease = t * t * (3f - 2f * t);
            consolePanel.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, ease));
            yield return null;
        }
        consolePanel.anchoredPosition = new Vector2(0, targetY);

        if (!show) consolePanel.gameObject.SetActive(false);
    }

    // 엔터키를 쳤을 때 명령어 분석 및 실행!
    private void OnSubmitCommand(string input)
    {
        // 닫힐 때 등 빈칸이면 무시
        if (string.IsNullOrWhiteSpace(input) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            inputField.ActivateInputField();
            return;
        }

        string[] args = input.Trim().Split(' ');
        string command = args[0].ToLower(); // 소문자로 통일해서 비교

        Player player = FindObjectOfType<Player>();

        switch (command)
        {
            case "stat": // 예: stat hp 10, stat dmg 5, stat spd 2
                if (args.Length < 3) { Log("사용법: stat [hp/dmg/spd] [숫자]"); break; }
                float val = float.Parse(args[2]);
                if (args[1] == "hp") { player.maxHealth += (int)val; player.Heal((int)val); Log($"최대 체력 {val} 증가!"); }
                else if (args[1] == "dmg") { player.attackDamage += val; Log($"공격력 {val} 증가!"); }
                else if (args[1] == "spd") { FindObjectOfType<PlayerController>().moveSpeed += val; Log($"이동속도 {val} 증가!"); }
                break;

            case "floor": // 예: floor 3
                if (args.Length < 2) { Log("사용법: floor [층숫자]"); break; }
                int targetFloor = int.Parse(args[1]);
                MapGenerator.Instance.currentFloor = targetFloor - 1; // 1 빼주는 이유는 맵 넘어가면서 +1 되기 때문
                Log($"{targetFloor}층으로 이동합니다!");
                ToggleConsole(); // 콘솔 끄고
                MapGenerator.Instance.GoToNextStage(); // 층 이동 실행!
                break;

            case "coin": // 예: coin 50
                if (args.Length < 2) { Log("사용법: coin [숫자]"); break; }
                player.AddCoin(int.Parse(args[1]));
                Log($"코인을 {args[1]}개 획득했습니다!");
                break;

            case "tp": // 예: tp boss, tp item, tp shop
                if (args.Length < 2) { Log("사용법: tp [boss/item/shop]"); break; }
                RoomController[] rooms = FindObjectsOfType<RoomController>();
                bool found = false;
                foreach (var room in rooms)
                {
                    if ((args[1] == "boss" && room.isBossRoom) ||
                        (args[1] == "item" && room.isItemRoom) ||
                        (args[1] == "shop" && room.isShopRoom))
                    {
                        // ★ [수정됨] 좌표(transform.position)가 아니라 room 자체를 넘겨줍니다!
                        RoomManager.Instance.CheatTeleport(room);

                        room.VisitRoom(); // 미니맵 밝히기
                        Log($"{args[1]} 방으로 순간이동 완료!");
                        found = true;
                        ToggleConsole(); // 이동 후 콘솔 끄기
                        break;
                    }
                }
                if (!found) Log("해당 방을 찾을 수 없습니다.");
                break;

            case "item": // 예: item 날개 달린 신발 5
                if (args.Length < 2) { Log("사용법: item [아이템이름] [개수(선택)]"); break; }

                int itemCount = 1;
                string targetItemName = "";

                // 마지막 띄어쓰기 부분이 숫자인지 확인 (개수 파악)
                if (int.TryParse(args[args.Length - 1], out int parsedCount))
                {
                    itemCount = parsedCount;
                    // 숫자 앞부분까지 전부 합쳐서 이름으로 만듦 (띄어쓰기 무시)
                    targetItemName = string.Join("", args, 1, args.Length - 2).Replace(" ", "");
                }
                else
                {
                    // 숫자가 없으면 전체가 이름
                    targetItemName = string.Join("", args, 1, args.Length - 1).Replace(" ", "");
                }

                bool itemFound = false;
                foreach (ItemData itemData in MapGenerator.Instance.possibleItems)
                {
                    if (itemData.itemName.Replace(" ", "") == targetItemName)
                    {
                        // ★ [수정됨] 개수(itemCount)를 같이 넘겨줍니다!
                        player.AcquireItem(itemData, itemCount);
                        ItemUIManager.Instance.ShowItemGet(itemData, itemCount);

                        Log($"{itemData.itemName} {itemCount}개 획득 완료!");
                        itemFound = true;
                        break;
                    }
                }
                if (!itemFound) Log("아이템 이름을 다시 확인해주세요.");
                break;

            case "achieve": // 예: achieve BadEnding1
                if (args.Length < 2) { Log("사용법: achieve [업적ID]"); break; }
                AchievementManager.Instance.UnlockAchievement(args[1]);
                Log($"{args[1]} 업적 잠금해제 명령 전송!");
                break;

            case "killall": // 예: killall
                RoomController[] allRooms = FindObjectsOfType<RoomController>();
                RoomController currentRoom = null;
                float closestDist = float.MaxValue;

                // 1. 물리 충돌(Trigger) 여부와 상관없이, 플레이어와 가장 가까운 방을 강제로 찾습니다.
                foreach (var room in allRooms)
                {
                    float dist = Vector2.Distance(player.transform.position, room.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        currentRoom = room;
                    }
                }

                if (currentRoom != null)
                {
                    int killCount = 0;
                    // 2. 데미지를 999999로 올려서 체력이 아무리 높아도 무조건 즉사시킵니다.
                    for (int i = currentRoom.enemiesInRoom.Count - 1; i >= 0; i--)
                    {
                        if (currentRoom.enemiesInRoom[i] != null)
                        {
                            currentRoom.enemiesInRoom[i].TakeDamage(999999f);
                            killCount++;
                        }
                    }
                    Log($"현재 방의 적 {killCount}마리를 즉사시켰습니다!");
                }
                else
                {
                    Log("방을 찾을 수 없습니다.");
                }
                break;

            case "godmode": // 예: godmode
                player.godMode = !player.godMode;
                Log(player.godMode ? "무적 모드가 활성화되었습니다!" : "무적 모드가 해제되었습니다.");
                break;
            // ==========================================
            // ★ [새로 추가된 명령어들]
            // ==========================================
            case "help":
                Log("--- [사용 가능한 명령어 목록] ---");
                Log("stat [hp/dmg/spd] [숫자] : 해당 스탯 증가");
                Log("floor [숫자] : 해당 층으로 맵 재생성");
                Log("coin [숫자] : 지정한 개수만큼 코인 획득");
                Log("tp [boss/item/shop] : 해당 특수 방으로 순간이동");
                // ★ [설명 수정됨]
                Log("item [아이템이름] [개수] : 해당 아이템을 숫자만큼 획득 (숫자 생략시 1개)");
                Log("achieve [업적ID] : 도전과제 강제 해금");
                Log("killall : 현재 방에 있는 모든 적(보스 포함) 즉사");
                Log("godmode : 치트 무적 모드 ON/OFF");
                Log("clear : 콘솔 창 기록 지우기");
                Log("spawn [coin/key/heart] [숫자] : 내 위치에 드랍 아이템 소환");
                break;

            case "clear": // 로그 청소 명령어
                logText.text = "[개발자 콘솔 활성화]"; // 텍스트 초기화
                break;

            // ==========================================

            // ==========================================
            // ★ [수정된 spawn 명령어]
            // ==========================================
            case "spawn": // 예: spawn coin 10
                if (args.Length < 2) { Log("사용법: spawn [coin/key/heart] [개수]"); break; }

                int count = 1;
                if (args.Length >= 3) int.TryParse(args[2], out count);

                GameObject targetPrefab = null;
                if (args[1] == "coin") targetPrefab = spawnCoinPrefab;
                else if (args[1] == "key") targetPrefab = spawnKeyPrefab;
                else if (args[1] == "heart") targetPrefab = spawnHeartPrefab;

                if (targetPrefab != null)
                {
                    // ★ [핵심] 현재 플레이어가 있는 방의 정중앙 좌표를 수학적으로 계산합니다!
                    float rWidth = MapGenerator.Instance.roomWidth;
                    float rHeight = MapGenerator.Instance.roomHeight;

                    // 플레이어의 현재 위치를 방 크기로 나눠 반올림하면 현재 방의 중심 좌표가 딱 나옵니다.
                    float centerX = Mathf.Round(player.transform.position.x / rWidth) * rWidth;
                    float centerY = Mathf.Round(player.transform.position.y / rHeight) * rHeight;
                    Vector3 roomCenter = new Vector3(centerX, centerY, 0);

                    // 지정된 개수만큼 '방 정중앙'에 소환!
                    for (int i = 0; i < count; i++)
                    {
                        Instantiate(targetPrefab, roomCenter, Quaternion.identity);
                    }
                    Log($"{args[1]} {count}개 방 중앙에 스폰 완료!");
                }
                else
                {
                    Log("알 수 없는 아이템입니다. (coin, key, heart 중 선택)");
                }
                break;

            default:
                Log("알 수 없는 명령어입니다.");
                break;
        }

        // 실행 후 입력창 비우고 다시 포커스 맞추기
        inputField.text = "";
        inputField.ActivateInputField();
    }

    // 로그 텍스트를 UI에 출력하는 함수
    // ==========================================
    // ★ [수정됨] 로그 텍스트를 UI에 출력하는 함수
    // ==========================================
    private void Log(string msg)
    {
        logText.text += "\n> " + msg;

        // 글씨가 추가되면 스크롤을 맨 아래로 쫙 내려줌!
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    // ★ [새로 추가됨] 스크롤 맨 아래로 내리기 코루틴
    private IEnumerator ScrollToBottom()
    {
        // 유니티 UI가 글씨 크기를 계산할 시간을 벌어주기 위해 딱 1프레임 대기합니다.
        yield return null;

        if (logScrollRect != null)
        {
            // 0이 맨 아래, 1이 맨 위입니다.
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}