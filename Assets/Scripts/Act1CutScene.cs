using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Hill Act 1 intro + end-of-level cutscene.
///
/// AT THE START OF THE SCENE:
///   "HILL ACT 1" title card on black, game frozen, then gameplay begins.
///
/// AT THE END OF THE LEVEL:
///   1. Tails crosses the trigger (a bit away from Sonic): music stops, Tails stops,
///      static fades in
///   2. After a short pause, Tails slowly walks toward Sonic (static stays on)
///   3. Tails arrives: Sonic's sprite changes (open eye = it's Sonic.exe)
///   4. Hard cut to black
///   5. The "HELLO, DO YOU WANT TO PLAY WITH ME?" image appears WITH its audio
///   6. Load the escape scene (it will have its own "HIDE AND SEEK" title card)
///
/// Put this on an empty GameObject in the Act 1 scene.
/// </summary>
public class Act1Cutscene : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private TailsMovement tails;
    [Tooltip("Sonic in the scene. Leave him DISABLED - the script turns him on.")]
    [SerializeField] private GameObject sonic;

    [Header("Distances (X axis, between Tails and Sonic)")]
    [Tooltip("Sonic pops into view when Tails gets this close.")]
    [SerializeField] private float sonicAppearDistance = 20f;
    [Tooltip("The cutscene trigger. Tails crossing this distance starts the cutscene.")]
    [SerializeField] private float cutsceneStartDistance = 14f;
    [Tooltip("Tails walks until he is this close to Sonic, then the reveal happens.")]
    [SerializeField] private float arriveDistance = 1.5f;

    [Header("Tails During the Cutscene")]
    [Tooltip("Seconds Tails stands still after the trigger (the static fades in during this).")]
    [SerializeField] private float stopTime = 1.5f;
    [Tooltip("How fast Tails walks toward Sonic (0 = stands still, 1 = full speed).")]
    [SerializeField, Range(0f, 1f)] private float walkSpeed = 0.25f;
    [Tooltip("Safety: if Tails hasn't reached Sonic after this many seconds, continue anyway.")]
    [SerializeField] private float walkTimeout = 20f;

    [Header("Sonic Reveal (open eye = Sonic.exe)")]
    [Tooltip("Optional. Leave empty to find it automatically on Sonic.")]
    [SerializeField] private SpriteRenderer sonicRenderer;
    [Tooltip("Sprites Sonic changes to when Tails arrives. 1 sprite = instant swap. More = plays them in order and stays on the last.")]
    [SerializeField] private Sprite[] sonicRevealFrames;
    [SerializeField] private float revealFrameTime = 0.15f;
    [Tooltip("How long we stay on the revealed Sonic before cutting to black.")]
    [SerializeField] private float revealHoldTime = 1.5f;

    [Header("Intro Title Card (start of the scene)")]
    [Tooltip("Parent object holding the 'HILL ACT 1' text and the pink oval.")]
    [FormerlySerializedAs("titleCard")]
    [SerializeField] private GameObject introTitleCard;
    [Tooltip("How long the title card stays up. 0 = no intro.")]
    [SerializeField] private float introTitleTime = 3f;

    [Header("UI (all on one Canvas, drawn above the HUD)")]
    [Tooltip("A full-screen Image. The script swaps its sprite to animate the static.")]
    [SerializeField] private Image staticOverlay;
    [SerializeField] private Image blackScreen;
    [Tooltip("An Image showing 'HELLO, DO YOU WANT TO PLAY WITH ME?'")]
    [SerializeField] private Image messageImage;

    [Header("Static Animation")]
    [Tooltip("Drag your 3 static sprites here, in order.")]
    [SerializeField] private Sprite[] staticFrames;
    [Tooltip("Seconds each frame is shown. Smaller = more frantic.")]
    [SerializeField] private float staticFrameTime = 0.06f;
    [Tooltip("How visible the static gets at its strongest (1 = fully opaque).")]
    [SerializeField, Range(0f, 1f)] private float staticMaxAlpha = 0.5f;
    [SerializeField] private float staticFadeInTime = 1.5f;

    [Header("Timing (seconds)")]
    [SerializeField] private float blackTime = 5f;
    [Tooltip("0 = the message image appears instantly.")]
    [SerializeField] private float messageFadeInTime = 0f;
    [Tooltip("Minimum time the message stays up. It always waits for the audio to finish too.")]
    [SerializeField] private float messageHoldTime = 4f;

    [Header("Audio")]
    [Tooltip("The Audio Source playing the level's background music. It stops when the cutscene starts.")]
    [SerializeField] private AudioSource music;
    [Tooltip("0 = the music cuts off instantly.")]
    [SerializeField] private float musicFadeOutTime = 0f;
    [Tooltip("Optional. If empty, the script creates its own Audio Source. Must NOT be the music one.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Static noise, loops from the moment the cutscene starts until the cut to black.")]
    [SerializeField] private AudioClip staticLoop;
    [Tooltip("The 'Hello, do you want to play with me?' audio. Plays together with the message image.")]
    [SerializeField] private AudioClip messageSound;

    [Header("Next Scene")]
    [Tooltip("Must be added in File > Build Settings.")]
    [SerializeField] private string escapeSceneName = "EscapeScene";

    private bool introDone;
    private bool started;
    private bool froze;

    private void Awake()
    {
        // Make sure the UI objects are on and ready, then hide them with alpha
        Prepare(staticOverlay);
        Prepare(blackScreen);
        Prepare(messageImage);

        SetAlpha(staticOverlay, 0f);
        SetAlpha(blackScreen, 0f);
        SetAlpha(messageImage, 0f);

        if (introTitleCard != null) introTitleCard.SetActive(false);
        if (sonic != null) sonic.SetActive(false);

        if (staticOverlay != null && staticFrames != null && staticFrames.Length > 0)
            staticOverlay.sprite = staticFrames[0];

        // Cutscene sounds need their own Audio Source (never the music one)
        if (audioSource == null || audioSource == music)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        WarnAboutMissingReferences();
    }

    private IEnumerator Start()
    {
        yield return IntroTitleCard();
        introDone = true;
    }

    private void OnDestroy()
    {
        // Never leave the game frozen if we're destroyed during the intro
        if (froze) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!introDone || started || tails == null || sonic == null) return;

        float distance = DistanceToSonic();

        if (!sonic.activeSelf && distance < sonicAppearDistance)
            sonic.SetActive(true);

        if (distance < cutsceneStartDistance)
        {
            started = true;
            StartCoroutine(PlayCutscene());
        }
    }

    // ---------- Intro: "HILL ACT 1" ----------

    private IEnumerator IntroTitleCard()
    {
        if (introTitleCard == null || introTitleTime <= 0f)
            yield break;

        // Black background with the title card on top, game frozen underneath
        SetAlpha(blackScreen, 1f);
        introTitleCard.SetActive(true);

        if (tails != null) tails.SetControl(false, 0f);
        Time.timeScale = 0f;
        froze = true;

        yield return new WaitForSecondsRealtime(introTitleTime);

        Time.timeScale = 1f;
        froze = false;

        introTitleCard.SetActive(false);
        SetAlpha(blackScreen, 0f);
        if (tails != null) tails.SetControl(true);
    }

    // ---------- The end-of-level cutscene ----------

    private IEnumerator PlayCutscene()
    {
        Debug.Log("[Act1Cutscene] Cutscene started.");
        sonic.SetActive(true);

        // 1. Music stops. Tails stops. Static fades in.
        StartCoroutine(StopMusic());
        tails.SetControl(false, 0f);

        Coroutine staticAnim = StartCoroutine(StaticAnimationRoutine());
        Coroutine staticFade = StartCoroutine(FadeStaticIn());
        PlaySound(staticLoop, loop: true);

        yield return new WaitForSeconds(stopTime);

        // 2. Tails slowly walks to Sonic (static stays on)
        float direction = Mathf.Sign(sonic.transform.position.x - tails.transform.position.x);
        tails.SetControl(false, direction * walkSpeed);

        float walked = 0f;
        while (DistanceToSonic() > arriveDistance && walked < walkTimeout)
        {
            walked += Time.deltaTime;
            yield return null;
        }
        tails.SetControl(false, 0f);

        // 3. Sonic's eye opens - it's Sonic.exe
        yield return RevealSonic();
        yield return new WaitForSeconds(revealHoldTime);

        // 4. Hard cut to black
        StopCoroutine(staticFade);
        StopCoroutine(staticAnim);
        StopSound();
        SetAlpha(staticOverlay, 0f);
        SetAlpha(blackScreen, 1f);
        yield return new WaitForSeconds(blackTime);

        // 5. Message image + its audio, at the same time
        PlaySound(messageSound, loop: false);
        yield return FadeIn(messageImage, messageFadeInTime);

        float hold = messageHoldTime;
        if (messageSound != null) hold = Mathf.Max(hold, messageSound.length);
        yield return new WaitForSeconds(hold);
        SetAlpha(messageImage, 0f);

        // 6. Go to the escape scene (it shows "HIDE AND SEEK" itself)
        SceneManager.LoadScene(escapeSceneName);
    }

    private float DistanceToSonic()
    {
        return Mathf.Abs(sonic.transform.position.x - tails.transform.position.x);
    }

    // ---------- Sonic reveal ----------

    private IEnumerator RevealSonic()
    {
        if (sonicRevealFrames == null || sonicRevealFrames.Length == 0)
            yield break;

        SpriteRenderer sr = sonicRenderer != null ? sonicRenderer : sonic.GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning("[Act1Cutscene] Sonic has no SpriteRenderer, can't change his sprite.", this);
            yield break;
        }

        // An Animator would overwrite our sprite every frame, so turn it off
        Animator anim = sr.GetComponent<Animator>();
        if (anim == null) anim = sonic.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        for (int i = 0; i < sonicRevealFrames.Length; i++)
        {
            sr.sprite = sonicRevealFrames[i];
            if (i < sonicRevealFrames.Length - 1)
                yield return new WaitForSeconds(revealFrameTime);
        }
    }

    // ---------- Static ----------

    private IEnumerator StaticAnimationRoutine()
    {
        if (staticOverlay == null || staticFrames == null || staticFrames.Length == 0)
            yield break;

        var wait = new WaitForSeconds(staticFrameTime);
        int frame = 0;

        while (true)
        {
            staticOverlay.sprite = staticFrames[frame];
            frame = (frame + 1) % staticFrames.Length;   // 0, 1, 2, 0, 1, 2...
            yield return wait;
        }
    }

    private IEnumerator FadeStaticIn()
    {
        float t = 0f;
        while (t < staticFadeInTime)
        {
            t += Time.deltaTime;
            SetAlpha(staticOverlay, Mathf.Clamp01(t / staticFadeInTime) * staticMaxAlpha);
            yield return null;
        }
        SetAlpha(staticOverlay, staticMaxAlpha);
    }

    // ---------- Music ----------

    private IEnumerator StopMusic()
    {
        if (music == null) yield break;

        if (musicFadeOutTime <= 0f)
        {
            music.Stop();
            yield break;
        }

        float startVolume = music.volume;
        float t = 0f;
        while (t < musicFadeOutTime)
        {
            t += Time.deltaTime;
            music.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeOutTime);
            yield return null;
        }

        music.Stop();
        music.volume = startVolume;
    }

    // ---------- Helpers ----------

    private IEnumerator FadeIn(Graphic graphic, float time)
    {
        if (time <= 0f)
        {
            SetAlpha(graphic, 1f);
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetAlpha(graphic, Mathf.Clamp01(t / time));
            yield return null;
        }
        SetAlpha(graphic, 1f);
    }

    private static void Prepare(Graphic graphic)
    {
        if (graphic == null) return;
        graphic.gameObject.SetActive(true);
        graphic.enabled = true;
        graphic.raycastTarget = false;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    private void PlaySound(AudioClip clip, bool loop)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    private void StopSound()
    {
        if (audioSource != null) audioSource.Stop();
    }

    private void WarnAboutMissingReferences()
    {
        if (tails == null) Debug.LogWarning("[Act1Cutscene] 'Tails' is not assigned.", this);
        if (sonic == null) Debug.LogWarning("[Act1Cutscene] 'Sonic' is not assigned.", this);
        if (staticOverlay == null) Debug.LogWarning("[Act1Cutscene] 'Static Overlay' is not assigned - no static will show.", this);
        if (staticFrames == null || staticFrames.Length == 0) Debug.LogWarning("[Act1Cutscene] 'Static Frames' is empty - drag your 3 static sprites in.", this);
        if (blackScreen == null) Debug.LogWarning("[Act1Cutscene] 'Black Screen' is not assigned.", this);
        if (messageImage == null) Debug.LogWarning("[Act1Cutscene] 'Message Image' is not assigned.", this);
        if (messageSound == null) Debug.LogWarning("[Act1Cutscene] 'Message Sound' is empty - the 'play with me' audio won't play.", this);
        if (music == null) Debug.LogWarning("[Act1Cutscene] 'Music' is not assigned - the music won't stop.", this);
        if (sonicRevealFrames == null || sonicRevealFrames.Length == 0) Debug.LogWarning("[Act1Cutscene] 'Sonic Reveal Frames' is empty - Sonic won't change sprite.", this);
        if (arriveDistance >= cutsceneStartDistance) Debug.LogWarning("[Act1Cutscene] 'Arrive Distance' should be smaller than 'Cutscene Start Distance'.", this);
        if (sonicAppearDistance < cutsceneStartDistance) Debug.LogWarning("[Act1Cutscene] 'Sonic Appear Distance' should be bigger than 'Cutscene Start Distance'.", this);
    }
}