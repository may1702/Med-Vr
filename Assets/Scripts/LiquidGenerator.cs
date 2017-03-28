using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uFlex {
    public class LiquidGenerator : MonoBehaviour
    {
        private const int LIQUID_LAYER = 10;
        GameObject Syringe;
        public static ArrayList particles = new ArrayList();
        public static ArrayList liquids = new ArrayList();
        SteamVR_TrackedController controller;

        // Use this for initialization
        void Start()
        {
            Syringe = gameObject;
            controller = gameObject.transform.parent.GetComponent<SteamVR_TrackedController>();
        }

        int count = 0;
        bool shouldSpawn = true;
        bool trigger = false;
        // Update is called once per frame
        void Update()
        {
            int dimX = 1;
            int dimY = 1;
            int dimZ = 1;

            float size = 0.5f;
            float spacing = 0.5f;


            if (count > 2)
            {
                count = 0;
                shouldSpawn = true;
            }

            trigger = controller.triggerPressed;

            if (shouldSpawn && trigger)
            {
                GameObject liquid = new GameObject("liquid");
                liquid.SetActive(false);
                liquid.transform.position = new Vector3(Syringe.transform.position.x, Syringe.transform.position.y, Syringe.transform.position.z);

                int particlesCount = dimX * dimY * dimZ;

                FlexParticles part = liquid.AddComponent<FlexParticles>();
                part.m_particlesCount = particlesCount;
                part.m_maxParticlesCount = particlesCount;
                part.m_particles = new Particle[particlesCount];
                part.m_velocities = new Vector3[particlesCount];
                part.m_restParticles = new Particle[particlesCount];
                part.m_smoothedParticles = new Particle[particlesCount];
                part.m_phases = new int[particlesCount];
                part.m_particlesActivity = new bool[particlesCount];
                part.m_densities = new float[particlesCount];
                part.m_colours = new Color[particlesCount];
                part.m_colour = Color.blue;
                part.m_interactionType = FlexInteractionType.Fluid;
                part.m_collisionGroup = -1;
                part.m_bounds.SetMinMax(new Vector3(), new Vector3(dimX * spacing, dimY * spacing, dimZ * spacing));
                part.m_type = FlexBodyType.Fluid;

                part.m_initialVelocity = Syringe.transform.forward.normalized * 12f;
                part.m_collisionGroup = 1;

                int i = 0;
                float invMass = 1.0f / 1;
                for (int x = 0; x < dimX; x++)
                {
                    for (int y = 0; y < dimY; y++)
                    {
                        for (int z = 0; z < dimZ; z++)
                        {

                            part.m_particles[i].pos = new Vector3(x, y, z) * spacing;
                            part.m_particles[i].invMass = invMass;
                            //   flexBody.m_colours[i] = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1.0f);
                            part.m_colours[i] = Color.blue;
                            part.m_particlesActivity[i] = true;

                            part.m_restParticles[i] = part.m_particles[i];
                            part.m_smoothedParticles[i] = part.m_particles[i];

                            //part.m_phases[i] = (int)phase;


                            if (0 != 0)
                                part.m_particles[i].pos += UnityEngine.Random.insideUnitSphere * 0;

                            i++;
                        }
                    }
                }

                particles.Add(part);
                liquids.Add(liquid);

                liquid.AddComponent<FlexParticlesRenderer>();
                liquid.GetComponent<FlexParticlesRenderer>().m_size = size;
                liquid.GetComponent<FlexParticlesRenderer>().m_radius = 0.1f;
                //liquid.GetComponent<FlexParticlesRenderer>().m_minDensity = 0.01f;
                //liquid.GetComponent<FlexParticlesRenderer>().m_showDensity = true;
                shouldSpawn = false;

                liquid.AddComponent<SphereCollider>();
                liquid.GetComponent<SphereCollider>().radius = 0.22f;
                liquid.AddComponent<Rigidbody>();
                liquid.GetComponent<Rigidbody>().useGravity = false;
                liquid.GetComponent<Rigidbody>().isKinematic = false;
                liquid.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                liquid.SetActive(true);

                liquid.layer = LIQUID_LAYER;
            }

            for (int y = 0; y < liquids.Count; y++)
            {
                GameObject liquidObj = liquids[y] as GameObject;
                Particle particle = liquidObj.GetComponent<FlexParticles>().m_particles[0];

                if (particle.pos.x == 0 && particle.pos.y == 0 && particle.pos.z == 0)
                    continue;

                liquidObj.transform.position = particle.pos;
            }
            count++;
        }
    }
    
}
