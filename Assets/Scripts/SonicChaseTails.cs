using UnityEngine;
using System.Collections;

public class SonicChaseTails : MonoBehaviour
{
    [Header("Movement")]
    public float Speed = 5f;

    [Header("Tails")]
    public GameObject Tails;
    public bool TailsIsDead = false;

    // New flag to freeze Sonic on impact
    private bool shouldStopSonic = false;

    private void Update()
    {
        // Sonic will ONLY move if Tails isn't dead AND Sonic isn't supposed to stop
        if (!TailsIsDead && !shouldStopSonic && Tails != null)
        {
            transform.Translate(Vector3.right * Speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D tails)
    {
        if (tails.gameObject == Tails && !TailsIsDead)
        {
            StartCoroutine(DeathTails());
        }
    }

    private IEnumerator DeathTails()
    {
        TailsIsDead = true;
        shouldStopSonic = true;

        yield return new WaitForSeconds(1f);
    }
}
