using System.Collections.Generic;
using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticCharger : MonoBehaviour
    {
        [Tooltip("Use 1 for the red positive charger and -1 for the blue negative charger.")]
        public int polarity = 1;
        public int maxAbsCharge = 6;
        public float chargeInterval = 0.55f;
        public float effectiveRadius = 1.15f;

        readonly Dictionary<ElectrostaticCharge, float> timers = new Dictionary<ElectrostaticCharge, float>();

        void Update()
        {
            ElectrostaticCharge[] charges = FindObjectsOfType<ElectrostaticCharge>();
            for (int i = 0; i < charges.Length; i++)
            {
                ElectrostaticCharge charge = charges[i];
                if (charge == null)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, charge.transform.position) <= effectiveRadius)
                {
                    AccumulateCharge(charge, Time.deltaTime);
                }
            }
        }

        void OnTriggerStay(Collider other)
        {
            ElectrostaticCharge charge = other.GetComponentInParent<ElectrostaticCharge>();
            if (charge == null)
            {
                return;
            }

            AccumulateCharge(charge, Time.deltaTime);
        }

        public void ChargeImmediately(ElectrostaticCharge charge)
        {
            if (charge == null)
            {
                return;
            }

            charge.AddCharge(polarity >= 0 ? 1 : -1, maxAbsCharge);
            timers[charge] = 0f;
        }

        void AccumulateCharge(ElectrostaticCharge charge, float deltaTime)
        {
            float elapsed;
            timers.TryGetValue(charge, out elapsed);
            elapsed += deltaTime;

            if (elapsed >= chargeInterval)
            {
                charge.AddCharge(polarity >= 0 ? 1 : -1, maxAbsCharge);
                elapsed = 0f;
            }

            timers[charge] = elapsed;
        }

        void OnTriggerExit(Collider other)
        {
            ElectrostaticCharge charge = other.GetComponentInParent<ElectrostaticCharge>();
            if (charge != null)
            {
                timers.Remove(charge);
            }
        }
    }
}
