using UnityEngine;

namespace Lolopupka // Gleicher Namespace wie proceduralAnimation!
{
    public class SpiderBodyLift : MonoBehaviour
    {
        public proceduralAnimation legSystem;
        public Transform bodyTransform;
        public float liftSpeed = 3f;
        public float maxLift = 0.5f;
        public float baseHeight = 0.1f;

        private Vector3 initialLocalPosition;

        void Start()
        {
            if (bodyTransform == null)
                bodyTransform = transform;

            initialLocalPosition = bodyTransform.localPosition;
        }

        void Update()
        {
            if (legSystem == null) return;

            float legHeight = legSystem.GetAverageLegHeight();
            float targetY = Mathf.Clamp(legHeight, 0, maxLift) + baseHeight;

            Vector3 targetPos = new Vector3(
                initialLocalPosition.x,
                targetY,
                initialLocalPosition.z
            );

            bodyTransform.localPosition = Vector3.Lerp(bodyTransform.localPosition, targetPos, Time.deltaTime * liftSpeed);
        }
    }
}
