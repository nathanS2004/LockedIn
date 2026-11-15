using UnityEngine;

namespace LockedIn
{
    // Mirrors the SDK's centered sensor crop, with bottom-left viewport coordinates.
    public static class RoomCameraProjection
    {
        public static Vector4 Intrinsics(Vector2 focal, Vector2 principal, Vector2 sensor, Vector2 resolution)
        {
            Vector2 scale = resolution / sensor;
            scale /= Mathf.Max(scale.x, scale.y);
            Vector2 crop = sensor * scale;
            Vector2 offset = sensor * (Vector2.one - scale) * 0.5f;
            return new Vector4(focal.x / crop.x, focal.y / crop.y,
                (principal.x - offset.x) / crop.x, (principal.y - offset.y) / crop.y);
        }

        public static Vector2 Project(Vector3 cameraPoint, Vector4 intrinsics) => new Vector2(
            intrinsics.x * cameraPoint.x / cameraPoint.z + intrinsics.z,
            intrinsics.y * cameraPoint.y / cameraPoint.z + intrinsics.w);
    }
}
