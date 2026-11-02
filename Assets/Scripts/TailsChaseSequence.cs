using UnityEngine;
using System.Collections;

public class TailsChaseSequence : MonoBehaviour
{
    [Header("Characters")]
    public GameObject Tails;
    public GameObject Sonic;

    [Header("Tails Components")]
    public Rigidbody2D TailsRb;
    public TailsMovement TailsMovement;
    public Animator TailsAnimator;
    public SpriteRenderer TailsSprite;

    [Header("Sprites")]
    public Sprite CreepyTailsSprite;

    [Header("Movement")]
    public float TailsSpeed = 8f;
    public float SonicSpeed = 10f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip DrowningAudio;
    public AudioClip ScreamAudio;

    [Header("Timing")]
    public float ChaseDuration = 11f;

    [Header("Sonic Attack")]
    public SpriteRenderer SonicSprite;
    public Sprite SonicAttackSprite;
    public Vector2 AttackOffset = new Vector2(-0.8f, 0f);

    [Header("Effects")]
    public GameObject RedEffect;

    private bool sequenceStarted;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            StartSequence();
    }

    public void StartSequence()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        StartCoroutine(ChaseSequence());
    }

    private IEnumerator ChaseSequence()
    {
        // Disable normal movement and animation control.
        TailsMovement.enabled = false;

        Sonic.SetActive(true);
        RedEffect.SetActive(true);

        // Start the audio.
        audioSource.clip = DrowningAudio;
        audioSource.Play();

        float elapsed = 0f;

        // Chase for exactly the configured duration.
        while (elapsed < ChaseDuration)
        {
            // Tails moves at constant speed.
            TailsRb.linearVelocity = new Vector2(
                TailsSpeed,
                TailsRb.linearVelocity.y
            );

            // Sonic follows Tails but stays behind him.
            Vector3 targetPosition = Tails.transform.position
                + Vector3.left * 1.2f;

            Sonic.transform.position = Vector3.MoveTowards(
                Sonic.transform.position,
                targetPosition,
                SonicSpeed * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // The chase ends. Sonic attacks Tails!
        TailsRb.linearVelocity = Vector2.zero;

        // Stop Tails' animation so it doesn't override the death sprite.
        if (TailsAnimator != null)
        TailsAnimator.enabled = false;

        // Position Sonic beside Tails.
        Sonic.transform.position = Tails.transform.position
            + (Vector3)AttackOffset;

        // Change Sonic's sprite.
        SonicSprite.sprite = SonicAttackSprite;

        // Change Tails' sprite.
        TailsSprite.sprite = CreepyTailsSprite;

        // Stop drowning audio and play scream.
        audioSource.Stop();
        audioSource.PlayOneShot(ScreamAudio);
    }
}
