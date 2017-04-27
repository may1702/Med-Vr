using UnityEngine;

namespace uFlex
{
    public class LiquidPopper : MonoBehaviour
    {
        private const int LIQUID_LAYER = 20;
        SteamVR_TrackedController controller;

        // Use this for initialization
        void Start()
        {
            controller = gameObject.transform.parent.transform.parent.GetComponent<SteamVR_TrackedController>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.gameObject.layer.Equals(LIQUID_LAYER) && !controller.triggerPressed)
            {
                GameObject particle = collision.collider.gameObject;
                LiquidGenerator.liquids.Remove(particle);
                Particle[] parts = particle.GetComponent<FlexParticles>().m_particles;
                for (int i = 0; i < parts.Length; i++)
                {
                    LiquidGenerator.particles.Remove(parts[i]);
                }

                Destroy(particle);
            }
        }
    }
}