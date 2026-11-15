using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace LockedIn
{
    internal sealed class ScreenDepthSelection
    {
        private const float DepthTolerance = 0.025f;
        private const float SampleStep = 0.025f;
        private const float NormalProbe = 0.045f;
        private const float Padding = 0.035f;

        private readonly ARRaycastManager raycasts;
        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>(1);

        public ScreenDepthSelection(ARRaycastManager raycasts)
        {
            this.raycasts = raycasts;
        }

        public bool TryDepthPoint(Ray ray, out Vector3 point)
        {
            point = default;
            if (raycasts == null || !raycasts.enabled ||
                !raycasts.Raycast(ray, hits, TrackableType.Depth))
                return false;

            point = hits[0].pose.position;
            return true;
        }

        public IEnumerator FindScreen(Ray pointer, Vector3 eye, Vector3 viewRight, Action<ScreenVolume?> done)
        {
            yield return null;
            if (!TryDepthPoint(pointer, out Vector3 center) ||
                Vector3.Distance(eye, center) > 2f ||
                !TryFitPlane(eye, center, viewRight, out Vector3 right, out Vector3 up, out Vector3 normal))
            {
                done(null);
                yield break;
            }

            float[] extents = new float[4];
            Vector3[] directions = { -right, right, -up, up };
            for (int edge = 0; edge < directions.Length; edge++)
            {
                // Split the depth queries across frames. Two misses reject an isolated dropped sample.
                int misses = 0;
                for (int step = 1; step <= 28; step++)
                {
                    float distance = step * SampleStep;
                    Vector3 test = center + directions[edge] * distance;
                    if (IsOnPlane(eye, test, center, normal))
                    {
                        misses = 0;
                        extents[edge] = distance;
                    }
                    else if (++misses == 2)
                    {
                        break;
                    }

                    if (step % 6 == 0)
                        yield return null;
                }

                if (misses < 2 || extents[edge] < SampleStep * 2)
                {
                    done(null);
                    yield break;
                }
                yield return null;
            }

            float width = extents[0] + extents[1];
            float height = extents[2] + extents[3];
            if (width < 0.19f || width > 1.1f || height < 0.10f || height > 0.7f ||
                width / height < 1.1f || width / height > 3.5f)
            {
                done(null);
                yield break;
            }

            // A round or irregular object can pass the four central scans. Check insets near the corners.
            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner % 2 == 0 ? -extents[0] : extents[1];
                float y = corner < 2 ? -extents[2] : extents[3];
                if (!IsOnPlane(eye, center + (right * x + up * y) * 0.72f, center, normal))
                {
                    done(null);
                    yield break;
                }
            }

            done(new ScreenVolume(center, right, up, normal,
                new Vector3(-extents[0] - Padding, -extents[2] - Padding, -Padding),
                new Vector3(extents[1] + Padding, extents[3] + Padding, Padding)));
        }

        private bool TryFitPlane(Vector3 eye, Vector3 center, Vector3 viewRight,
                                 out Vector3 right, out Vector3 up, out Vector3 normal)
        {
            right = up = normal = default;
            Vector3 horizontal = Vector3.ProjectOnPlane(viewRight, Vector3.up).normalized * NormalProbe;
            Vector3 vertical = Vector3.up * NormalProbe;
            if (!TrySample(eye, center - horizontal, out Vector3 left) ||
                !TrySample(eye, center + horizontal, out Vector3 rightPoint) ||
                !TrySample(eye, center - vertical, out Vector3 bottom) ||
                !TrySample(eye, center + vertical, out Vector3 top))
                return false;

            Vector3 side = rightPoint - left;
            Vector3 rise = top - bottom;
            if (side.magnitude > 0.18f || rise.magnitude > 0.18f)
                return false;

            normal = Vector3.Cross(side, rise).normalized;
            if (normal.sqrMagnitude < 0.9f || Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.73f ||
                Mathf.Abs(Vector3.Dot(left - center, normal)) > DepthTolerance ||
                Mathf.Abs(Vector3.Dot(bottom - center, normal)) > DepthTolerance)
                return false;

            up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
            right = Vector3.Cross(up, normal).normalized;
            return true;
        }

        private bool IsOnPlane(Vector3 eye, Vector3 target, Vector3 center, Vector3 normal)
        {
            return TrySample(eye, target, out Vector3 hit) &&
                   Mathf.Abs(Vector3.Dot(hit - center, normal)) <= DepthTolerance &&
                   Vector3.Distance(hit, target) <= 0.08f;
        }

        private bool TrySample(Vector3 eye, Vector3 target, out Vector3 point)
        {
            Vector3 direction = (target - eye).normalized;
            // Start slightly forward of the headset to avoid sampling the headset's own near field.
            return TryDepthPoint(new Ray(eye + direction * 0.04f, direction), out point);
        }

        internal readonly struct ScreenVolume
        {
            public readonly Vector3 Origin, Right, Up, Normal, Min, Max;

            public ScreenVolume(Vector3 origin, Vector3 right, Vector3 up, Vector3 normal, Vector3 min, Vector3 max)
            {
                Origin = origin;
                Right = right;
                Up = up;
                Normal = normal;
                Min = min;
                Max = max;
            }
        }
    }
}
