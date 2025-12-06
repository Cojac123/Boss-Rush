using UnityEngine;
namespace CoryBoss
{
    public class LaserBeam : MonoBehaviour
    {
        public float speed = 25f;      // Fast travel like a beam
        public float lifetime = 2f;    // How long the beam exists

        void Start()
        {
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }
}