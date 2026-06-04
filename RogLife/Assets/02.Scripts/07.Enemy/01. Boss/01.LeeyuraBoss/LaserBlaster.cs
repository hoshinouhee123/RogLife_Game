using UnityEngine;
using System.Collections;
using UnityEngine.Audio; // ★ 믹서 사용을 위해 추가

public class LaserBlaster : MonoBehaviour
{
    [Header("레이저 부품 연결")]
    public SpriteRenderer moonRenderer;
    public SpriteRenderer warningRenderer;
    public SpriteRenderer laserRenderer;
    public BoxCollider2D laserCollider;

    [Header("타이밍 설정")]
    public float warningTime = 0.8f;
    public float fireDuration = 0.5f;

    // ==========================================
    // ★ [새로 추가됨] 레이저 발사 효과음
    // ==========================================
    [Header("사운드 설정")]
    public AudioClip fireSound;           // 콰아앙! 하는 레이저 소리
    public AudioMixerGroup sfxMixerGroup; // SFX 믹서 연결

    // ==========================================
    // ★ [새로 추가됨] 모든 레이저가 공유하는 '마지막으로 소리 낸 시간' 기억 장치
    // ==========================================
    private static float lastSoundTime = -1f;

    private float damage = 1f;

    private void Start()
    {
        Destroy(gameObject, warningTime + fireDuration + 0.5f);
    }

    // 셋업 시 커스텀 시간을 받도록 수정 (기존과 동일)
    public void Setup(float dmg, float customWarn = -1f, float customFire = -1f)
    {
        damage = dmg;
        if (customWarn > 0) warningTime = customWarn;
        if (customFire > 0) fireDuration = customFire;
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        laserCollider.enabled = false;
        laserRenderer.gameObject.SetActive(false);
        moonRenderer.color = Color.white;

        warningRenderer.gameObject.SetActive(true);
        warningRenderer.color = new Color(1f, 0f, 0f, 0.4f);

        yield return new WaitForSeconds(warningTime);

        if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.2f, 0.3f);

        warningRenderer.gameObject.SetActive(false);
        laserRenderer.gameObject.SetActive(true);
        laserRenderer.color = Color.white;
        laserCollider.enabled = true;

        // ==========================================
        // ★ [수정됨] 방금(0.1초 이내에) 누군가 소리를 냈는지 검사합니다!
        // ==========================================
        if (fireSound != null && Time.unscaledTime > lastSoundTime + 0.1f)
        {
            // 내가 소리를 냈다고 시간을 기록함 (나머지 3개는 이 기록을 보고 0.1초간 침묵함)
            lastSoundTime = Time.unscaledTime;

            GameObject audioObj = new GameObject("LaserAudio");
            audioObj.transform.position = transform.position;
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = fireSound;
            source.spatialBlend = 0f;
            if (sfxMixerGroup != null) source.outputAudioMixerGroup = sfxMixerGroup;
            source.Play();
            Destroy(audioObj, fireSound.length);
        }
        // ==========================================

        yield return new WaitForSeconds(fireDuration);

        laserCollider.enabled = false;
        float fadeTimer = 0f;
        while (fadeTimer < 0.2f)
        {
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / 0.2f);
            moonRenderer.color = new Color(1f, 1f, 1f, alpha);
            laserRenderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            collision.GetComponent<Player>().TakeDamage(Mathf.RoundToInt(damage));
    }
}