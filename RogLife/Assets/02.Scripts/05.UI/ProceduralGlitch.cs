using UnityEngine;
using UnityEngine.UI; // RawImage를 사용하기 위함
using System.Collections;

[RequireComponent(typeof(RawImage))]
public class ProceduralGlitch : MonoBehaviour
{
    private RawImage rawImage;
    private Texture2D noiseTexture;
    private Color[] pixels;

    [Header("노이즈 설정")]
    [Tooltip("해상도가 작을수록 픽셀이 굵은 도트(깍두기)처럼 보입니다.")]
    public int textureWidth = 256;  // 가로 픽셀 수
    public int textureHeight = 144; // 세로 픽셀 수
    public float fps = 30f;         // 1초에 노이즈가 몇 번 바뀔지 (속도)

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();

        // 코드로 직접 빈 캔버스(텍스처)를 만듭니다.
        noiseTexture = new Texture2D(textureWidth, textureHeight);

        // ★ 도트 게임 특유의 픽셀 깨짐 느낌을 살리기 위해 필터를 Point로 설정합니다.
        noiseTexture.filterMode = FilterMode.Point;
        rawImage.texture = noiseTexture;

        // 픽셀의 개수만큼 색상 배열을 준비합니다.
        pixels = new Color[textureWidth * textureHeight];
    }

    // 오브젝트가 켜질 때(글리치가 발동될 때) 코루틴 시작
    private void OnEnable()
    {
        StartCoroutine(GenerateNoiseRoutine());
    }

    private IEnumerator GenerateNoiseRoutine()
    {
        while (true)
        {
            // 모든 픽셀을 하나하나 돌면서 색상을 무작위로 바꿉니다!
            for (int i = 0; i < pixels.Length; i++)
            {
                // 1. 기본적으로 흑백 TV 노이즈 (지지직)
                float gray = Random.Range(0f, 1f);
                Color pixelColor = new Color(gray, gray, gray, 1f);

                // 2. 10% 확률로 끔찍한 빨간색/파란색 컬러 노이즈를 섞음!
                float randomVal = Random.value;
                if (randomVal > 0.95f)
                    pixelColor = new Color(Random.Range(0.5f, 1f), 0f, 0f, 1f); // 핏빛 빨강
                else if (randomVal > 0.90f)
                    pixelColor = new Color(0f, Random.Range(0f, 0.5f), Random.Range(0.5f, 1f), 1f); // 어두운 파랑

                pixels[i] = pixelColor;
            }

            // 텍스처에 바뀐 색상들을 덮어씌우고 화면에 적용!
            noiseTexture.SetPixels(pixels);
            noiseTexture.Apply();

            // 설정한 프레임 속도에 맞춰 대기 (시간 정지 중에도 작동하게 Realtime 사용)
            yield return new WaitForSecondsRealtime(1f / fps);
        }
    }
}