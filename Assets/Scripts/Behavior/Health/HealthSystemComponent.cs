﻿using UnityEngine;

namespace Behavior.Health {

    /// <summary>
    /// Adds a HealthSystem to a Game Object
    /// </summary>
    public class HealthSystemComponent : MonoBehaviour, IGetHealthSystem {

        [Tooltip("Max Health(No modify for MST)")]
        [SerializeField] private float healthAmountMax = 100f;

        [Tooltip("Starting Health amount, leave at 0 to start at full health.")]
        [SerializeField] private float startingHealthAmount = 0;

        private HealthSystem healthSystem;


        private void Awake() {
            // Create Health System
            healthSystem = new HealthSystem(healthAmountMax);

            if (startingHealthAmount != 0) {
                healthSystem.SetHealth(startingHealthAmount);
            }
        }

        /// <summary>
        /// Get the Health System created by this Component
        /// </summary>
        public HealthSystem GetHealthSystem() {
            return healthSystem;
        }


    }

}