using UnityEngine;

namespace Sorolla.Tutorial
{
    public class PointAtTarget : MonoBehaviour
    {
        public bool lockX = false;
        public bool lockY = false;
        public bool lockZ = false;
        public Vector3 target;

        private void LateUpdate()
        {
            if (target == Vector3.zero) return;

            Vector3 direction = target - transform.position;
            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            if (lockX || lockY || lockZ)
            {
                Vector3 targetEuler = targetRotation.eulerAngles;
                Vector3 currentEuler = transform.rotation.eulerAngles;

                if (lockX) targetEuler.x = currentEuler.x;
                if (lockY) targetEuler.y = currentEuler.y;
                if (lockZ) targetEuler.z = currentEuler.z;

                targetRotation = Quaternion.Euler(targetEuler);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }
}