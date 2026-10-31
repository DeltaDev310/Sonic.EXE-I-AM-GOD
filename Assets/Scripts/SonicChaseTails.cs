using UnityEngine;
using System.Collections;

public class SonicChaseTails : MonoBehaviour
{
    [Header("Movement")]
    public float Speed = 5f;

    [Header("Tails")]
    public GameObject Tails;
    public bool TailsIsDead = false;

    private void Update()
    {
        if (!TailsIsDead && Tails != null)
        {
            transform.Translate(Vector3.right * Speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D tails)
    {
        if (tails.CompareTag("Player") && !TailsIsDead)
        {
            StartCoroutine(DeathTails());
        }
    }

    private IEnumerator DeathTails()
    {
        TailsIsDead = true;

        Debug.Log("Tails is dead");

        Tails.GetComponent<TailsMovement>().enabled = false;

        yield return new WaitForSeconds(1f);
    }
}