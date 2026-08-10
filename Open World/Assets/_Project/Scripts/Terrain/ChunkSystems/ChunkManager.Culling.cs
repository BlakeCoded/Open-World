using Project.Singleton;
using UnityEngine;

namespace WorldGen.Terrain
{
    // ChunkManager.Culling.cs
    public partial class ChunkManager : MonoBehaviourSingleton<ChunkManager>
    {
        static readonly Plane[] s_planes = new Plane[6];
        static readonly Vector4[] s_planesVecs = new Vector4[6];
        static readonly float[] s_planeRadius = new float[6];

        Vector3 lastCameraPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        Quaternion lastCameraRot;

        Vector3 camPos;
        Vector3 camForward;
        Quaternion camRotation;

        private void CacheCamera()
        {
            var t = cam.transform;

            camPos = t.position;
            camForward = t.forward;
            camRotation = t.rotation;
        }

        private void UpdateVisibility()
        {
            var cameraChanged = camPos != lastCameraPos || camRotation != lastCameraRot;

            if(!cameraChanged) return;

            visibleChunks.Clear();

            CacheFrustumPlanes(cam);
            lastCameraPos = camPos;
            lastCameraRot = camRotation;

            foreach(var id in activeIds)
            {
                if(chunkDataByID.TryGetValue(id, out var chunk))
                {
                    UpdateChunkVisibilty(chunk);
                }
            }
        }

        private void UpdateChunkVisibilty(ChunkData chunk)
        {
            var toChunk = chunk.CullData.Center - camPos;
            var radius = chunk.CullData.Radius;

            var dot = Vector3.Dot(camForward, toChunk);

            // a simpler dot product test
            // if behind camera, offset by a chunks radius
            // turn off mesh renderer
            if(dot <= -radius) 
            {
                if(chunk.CullData.Visible)
                {
                    SetChunkVisible(chunk, false);
                }

                return;
            }

            var vis = TestChunk(chunk.CullData.Center);

            if(chunk.CullData.Visible != vis)
            {
                SetChunkVisible(chunk, vis);
            }

            if(chunk.CullData.Visible)
            {
                visibleChunks.Add(chunk.Coord);
            }
        }

        private void SetChunkVisible(ChunkData chunk, bool visible)
        {
            if(chunk.CullData.Visible == visible) return;

            chunk.CullData.Visible = visible;

            activeChunkViews[chunk.Coord].SetVisible(visible);
        }

        static readonly Vector3 ChunkExtents = new Vector3(ChunkSettings.ChunkSizeInUnits * 0.5f,
                                                           50f,
                                                           ChunkSettings.ChunkSizeInUnits * 0.5f);

        static readonly float ex = ChunkExtents.x;
        static readonly float ey = ChunkExtents.y;
        static readonly float ez = ChunkExtents.z;

        private void CacheFrustumPlanes(Camera cam)
        {
            // Build the camera's 6 frustum planes
            GeometryUtility.CalculateFrustumPlanes(cam, s_planes);

            for(int i = 0; i < 6; i++)
            {
                Vector3 n = s_planes[i].normal;

                // Cache the plane as a Vector4 (normal.xyz + distance)
                // so TestChunk() can use simple math without accessing Plane.
                s_planesVecs[i] = new Vector4(
                    n.x,
                    n.y,
                    n.z,
                    s_planes[i].distance);

                // Precompute how far a chunk's AABB extends along this plane.
                // This value is the same for every chunk because all chunks
                // have the same size.
                s_planeRadius[i] =
                    Mathf.Abs(n.x) * ex +
                    Mathf.Abs(n.y) * ey +
                    Mathf.Abs(n.z) * ez;
            }
        }

        private bool TestChunk(Vector3 center)
        {
            for(int i = 0; i < 6; i++)
            {
                Vector4 p = s_planesVecs[i];

                // Signed distance from the chunk center to the plane.
                float distance =
                    p.x * center.x +
                    p.y * center.y +
                    p.z * center.z +
                    p.w;

                // If the chunk's bounding box is completely behind
                // any plane, it cannot be visible.
                if (distance + s_planeRadius[i] < 0f)
                    return false;
            }

            return true;
        }
    }
}