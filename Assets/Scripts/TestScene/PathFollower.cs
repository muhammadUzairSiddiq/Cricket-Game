using UnityEngine;
using System.Collections;

namespace CricketGame
{
    /// <summary>
    /// Moves an object along a provided path of positions at a given speed.
    /// Designed to be attached to the instantiated ball at runtime.
    /// </summary>
    public class PathFollower : MonoBehaviour
    {
        [SerializeField] private float speed = 12f; // m/s along the path
        [SerializeField] private float arcHeight = 0.2f; // subtle cricket arc added on top of path
        [SerializeField] private bool faceVelocity = true;

        private Vector3[] path;
        private System.Action onComplete;

        public void Initialize(Vector3[] worldPath, float pathSpeed, float addedArcHeight, System.Action onDone)
        {
            path = worldPath;
            speed = pathSpeed;
            arcHeight = addedArcHeight;
            onComplete = onDone;
        }

        public void Begin()
        {
            if (path == null || path.Length < 2)
            {
                onComplete?.Invoke();
                Destroy(this);
                return;
            }
            StopAllCoroutines();
            StartCoroutine(FollowPath());
        }

        IEnumerator FollowPath()
        {
            // Precompute cumulative distances for smooth, non-zigzag motion
            float totalLen = 0f;
            float[] cum = new float[path.Length];
            cum[0] = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                totalLen += Vector3.Distance(path[i - 1], path[i]);
                cum[i] = totalLen;
            }
            if (totalLen < 0.0001f) { onComplete?.Invoke(); Destroy(this); yield break; }

            float traveled = 0f;

            while (traveled < totalLen)
            {
                traveled += speed * Time.deltaTime;
                float targetDist = Mathf.Clamp(traveled, 0f, totalLen);
                // find segment
                int seg = 0;
                while (seg < path.Length - 1 && cum[seg + 1] < targetDist) seg++;
                float segStart = cum[seg];
                float segEnd = cum[seg + 1];
                float segT = Mathf.InverseLerp(segStart, segEnd, targetDist);
                Vector3 a = path[seg];
                Vector3 b = path[seg + 1];
                Vector3 dir = (b - a).normalized;
                Vector3 pos = Vector3.Lerp(a, b, segT);
                pos.y += Mathf.Sin((targetDist / totalLen) * Mathf.PI) * arcHeight;
                transform.position = pos;
                if (faceVelocity && dir.sqrMagnitude > 0.0001f) transform.forward = Vector3.Lerp(transform.forward, dir, 0.5f);

                yield return null;
            }

            onComplete?.Invoke();
            Destroy(this);
        }
    }
}


