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

        //private void UpdateChunkVisibility()
        //{
        //    var t = cam.transform;

        //    var cameraChanged = t.position != lastCameraPos || t.rotation != lastCameraRot;

        //    if (!cameraChanged) return;

        //    visibleChunks.Clear();

        //    CacheFrustumPlanes(cam, s_planes);
        //    lastCameraPos = t.position;
        //    lastCameraRot = t.rotation;

        //    foreach(var id in activeIds)
        //    {
        //        if(!chunksById.TryGetValue(id, out var chunk)) continue;

        //        int dx = Mathf.Abs(chunk.Coord.x - currentChunk.x);
        //        int dz = Mathf.Abs(chunk.Coord.y - currentChunk.y);
        //        int cd = Mathf.Max(dx, dz);

        //        var toChunk = chunk.CullData.Bounds.center - t.position;

        //        var distance = toChunk.magnitude;
        //        var radius = chunk.CullData.Bounds.extents.magnitude; 

        //        var dot = Vector3.Dot(t.forward, toChunk / distance);

        //        var radiusOffset = radius / distance;

        //        var behind = dot < -radiusOffset;

        //        if (cd > chunkViewRadius || behind == true)
        //        {
        //            if(chunk.CullData.Visible)
        //            {
        //                chunk.SetMeshVisible(false);
        //            }

        //            continue;
        //        }

        //        var vis = GeometryUtility.TestPlanesAABB(s_planes, chunk.CullData.Bounds);

        //        if(chunk.CullData.Visible != vis) chunk.SetMeshVisible(vis);

        //        if (!vis) continue;

        //        visibleChunks.Add(id);
        //    }
        //}

        private void UpdateVisibility()
        {
            var t = cam.transform;
            var cameraChanged = t.position != lastCameraPos || t.rotation != lastCameraRot;

            if (!cameraChanged) return;

            CacheFrustumPlanes(cam);
            lastCameraPos = t.position;
            lastCameraRot = t.rotation;

            visibleChunks.Clear();

            foreach (var id in activeIds)
            {
                if (chunksById.TryGetValue(id, out var chunk))
                {
                    UpdateChunkVisibilty(chunk);
                }
            }
        }

        private void UpdateChunkVisibilty(ChunkData chunk)
        {
            var t = cam.transform;

            var toChunk = chunk.CullData.Center - t.position;
            var radius = chunk.CullData.Radius;

            var dot = Vector3.Dot(t.forward, toChunk);
            var behind = dot < -radius;

            if (behind)
            {
                if (chunk.CullData.Visible)
                {
                    chunk.SetMeshVisible(false);
                }

                return;
            }

            var vis = TestChunk(chunk.CullData.Center);

            if (chunk.CullData.Visible != vis)
            {
                chunk.SetMeshVisible(vis);
            }

            if (chunk.CullData.Visible)
            {
                visibleChunks.Add(chunk.Coord);
            }
        }

        private void CacheFrustumPlanes(Camera cam, Plane[] planesOut)
        {
            GeometryUtility.CalculateFrustumPlanes(cam, planesOut);

            for(int i = 0; i < 6; i++)
            {
                var n = planesOut[i].normal;
                s_planesVecs[i] = new Vector4(n.x, n.y, n.z, planesOut[i].distance);
            }
        }

        static readonly Vector3 ChunkExtents = new Vector3(ChunkSettings.ChunkSizeInUnits * 0.5f,
                                                           50f,
                                                           ChunkSettings.ChunkSizeInUnits * 0.5f);

        static readonly float ex = ChunkExtents.x;
        static readonly float ey = ChunkExtents.y;
        static readonly float ez = ChunkExtents.z;

        private void CacheFrustumPlanes(Camera cam)
        {
            GeometryUtility.CalculateFrustumPlanes(cam, s_planes);

            for (int i = 0; i < 6; i++)
            {
                Vector3 n = s_planes[i].normal;

                s_planesVecs[i] = new Vector4(
                    n.x,
                    n.y,
                    n.z,
                    s_planes[i].distance);

                s_planeRadius[i] =
                    Mathf.Abs(n.x) * ex +
                    Mathf.Abs(n.y) * ey +
                    Mathf.Abs(n.z) * ez;
            }
        }

        private bool TestChunk(Vector3 center)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector4 p = s_planesVecs[i];

                float distance =
                    p.x * center.x +
                    p.y * center.y +
                    p.z * center.z +
                    p.w;

                if (distance + s_planeRadius[i] < 0f)
                    return false;
            }

            return true;
        }
    }
}