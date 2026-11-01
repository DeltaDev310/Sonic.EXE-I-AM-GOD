using UnityEngine;

public class TriggerEXE : MonoBehaviour
{
    [Header("References")]
    public GameObject Sonic;
    public GameObject Tails;
    public SonicChaseTails sonicChaseScript;

    [Header("Movement")]
    public float Speed = 8f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (Sonic != null)
            {
                Sonic.SetActive(true);
            }

            if (Tails != null && Tails.TryGetComponent<TailsMovement>(out TailsMovement movement))
            {
                movement.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (Tails == null || sonicChaseScript == null)
            return;

        if (!sonicChaseScript.TailsIsDead)
        {
            Tails.transform.Translate(Vector3.right * Speed * Time.deltaTime);
        }
        if (sonicChaseScript.TailsIsDead)
        {
            Speed = 0f; // Stop Tails from moving when dead
        }
    }
}
