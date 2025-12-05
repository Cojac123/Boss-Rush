using UnityEngine;

public class ShockwaveMove : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 1.2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifetime);   
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
