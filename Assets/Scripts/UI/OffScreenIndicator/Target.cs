﻿using UnityEngine;

namespace UI.OffScreenIndicator
{
    /// <summary>
    /// Attach this script to all the target game objects in the scene.
    /// </summary>
    /// //Refactored by YongchunLi

    [DefaultExecutionOrder(0)]
    public class Target : MonoBehaviour
    {
        [Tooltip("Change this color to change the indicators color for this target")]
        [SerializeField] internal Color targetColor = Color.red;

        [Tooltip("Select if box indicator is required for this target")]
        [SerializeField] private bool needBoxIndicator = true;

        [Tooltip("Select if arrow indicator is required for this target")]
        [SerializeField] private bool needArrowIndicator = true;

        [Tooltip("Select if distance text is required for this target")]
        [SerializeField] private bool needDistanceText = true;

        [Tooltip("Box indicator positionTransform")] [SerializeField]
        internal Transform boxIndicatorDrawAtTransform;

        /// <summary>
        /// Please do not assign its value yourself without understanding its use.
        /// A reference to the target's indicator, 
        /// its value is assigned at runtime by the offscreen indicator script.
        /// </summary>
        [HideInInspector] public Indicator indicator;

        /// <summary>
        /// Gets the color for the target indicator.
        /// </summary>
        public Color TargetColor
        {
            get
            {
            return targetColor;
            }
        }

        internal Transform GetIndicatorDrawTransform()
        {
            return boxIndicatorDrawAtTransform ? boxIndicatorDrawAtTransform : transform;
        }

        /// <summary>
        /// Gets if box indicator is required for the target.
        /// </summary>
        public bool NeedBoxIndicator
        {
            get
            {
            return needBoxIndicator;
            }
            set => needBoxIndicator = value;
        }

        /// <summary>
        /// Gets if arrow indicator is required for the target.
        /// </summary>
        public bool NeedArrowIndicator
        {
            get
            {
            return needArrowIndicator;
            }
            set => needArrowIndicator = value;
        }

        /// <summary>
        /// Gets if the distance text is required for the target.
        /// </summary>
        public bool NeedDistanceText
        {
            get
            {
            return needDistanceText;
            }
            set => needDistanceText = value;
        }

        /// <summary>
        /// On enable add this target object to the targets list.
        /// </summary>
        private void OnEnable()
        {
            if(OffScreenIndicator.TargetStateChanged != null)
            {
                OffScreenIndicator.TargetStateChanged.Invoke(this, true);
            }
        }

        /// <summary>
        /// On disable remove this target object from the targets list.
        /// </summary>
        private void OnDisable()
        {
            if(OffScreenIndicator.TargetStateChanged != null)
            {
                OffScreenIndicator.TargetStateChanged.Invoke(this, false);
            }
        }

        /// <summary>
        /// Gets the distance between the camera and the target.
        /// </summary>
        /// <param name="cameraPosition">Camera position</param>
        /// <returns></returns>
        public float GetDistanceFromCamera(Vector3 cameraPosition)
        {
            float distanceFromCamera = Vector3.Distance(cameraPosition, transform.position);
            return distanceFromCamera;
        }
    }
}
