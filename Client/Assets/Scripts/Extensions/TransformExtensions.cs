using UnityEngine;

namespace Assets.Scripts.Extensions
{
    public static class TransformExtensions
    {
        public static void MoveTowardsTarget(this Transform transform, GameObject target, bool transformY = true, float speed = 3f)
        {
            var direction = target.transform.position - transform.position;

            if (!transformY)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            direction = direction.normalized;

            transform.position += direction * speed * Time.deltaTime;
        }

        public static bool IsCloseToTarget(this Transform transform, GameObject target, float distance = 0.5f)
        {
            return Vector3.Distance(transform.position, target.transform.position) < distance;
        }

        public static bool IsFarToTarget(this Transform transform, GameObject target, float distance = 15f)
        {
            return Vector3.Distance(transform.position, target.transform.position) > distance;
        }
    }
}
