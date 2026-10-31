using UnityEngine;

public class TriggerEXE : MonoBehaviour
{
    public GameObject Sonic;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {Sonic.SetActive(true);}
    }
}
