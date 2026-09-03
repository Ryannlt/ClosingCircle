using ClosingCircle.Domain;
using UnityEngine;

// Two ways to build the same wall, because the wall and the preview want opposite things.
//
// Build gives a unit mesh the wall scales by its transform, which is what lets it update every frame for free.
// BuildFitted gives a world-space mesh whose base sits on the ground at every segment, which the preview wants
// because it rebuilds only on change and its whole job is showing where the boundary meets the terrain.
//
// Both duplicate their vertices with flipped normals so the wall reads from inside as well, which saves
// depending on a shader that exposes a cull mode.

namespace ClosingCircle.Visual
{
    public static class ZoneMeshBuilder
    {
        // Unit radius and unit height, for a caller that scales it.
        public static Mesh Build(ZoneShape shape)
        {
            Vector2[] ring = shape.Vertices(Vector2.zero, 1f);
            var ground = new float[ring.Length];

            return Assemble(ring, ground, 1f, $"ClosingCircle_{shape.Sides}gon");
        }

        // World space, with one ground height per ring vertex. UV v stays 0 at the ground and 1 at the top, so
        // the alpha ramp is ground-relative by construction and every stage can share one material.
        public static Mesh BuildFitted(Vector2[] ring, float[] ground, float height) =>
            Assemble(ring, ground, height, "ClosingCircle_Fitted");

        private static Mesh Assemble(Vector2[] ring, float[] ground, float height, string name)
        {
            int sides = ring.Length;

            var vertices = new Vector3[sides * 8];
            var normals = new Vector3[sides * 8];
            var uvs = new Vector2[sides * 8];
            var triangles = new int[sides * 12];

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Vector2 a = ring[i];
                Vector2 b = ring[next];
                Vector3 outward = new Vector3(a.x + b.x, 0f, a.y + b.y).normalized;

                var bottomA = new Vector3(a.x, ground[i], a.y);
                var bottomB = new Vector3(b.x, ground[next], b.y);
                var topA = new Vector3(a.x, ground[i] + height, a.y);
                var topB = new Vector3(b.x, ground[next] + height, b.y);

                float u0 = (float)i / sides;
                float u1 = (float)(i + 1) / sides;

                int v = i * 8;
                Fill(vertices, normals, uvs, v, bottomA, bottomB, topA, topB, outward, u0, u1);
                Fill(vertices, normals, uvs, v + 4, bottomA, bottomB, topA, topB, -outward, u0, u1);

                int t = i * 12;
                Wind(triangles, t, v, flipped: false);
                Wind(triangles, t + 6, v + 4, flipped: true);
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Fill(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int v,
                                 Vector3 bottomA, Vector3 bottomB, Vector3 topA, Vector3 topB,
                                 Vector3 normal, float u0, float u1)
        {
            vertices[v] = bottomA;
            vertices[v + 1] = bottomB;
            vertices[v + 2] = topA;
            vertices[v + 3] = topB;

            uvs[v] = new Vector2(u0, 0f);
            uvs[v + 1] = new Vector2(u1, 0f);
            uvs[v + 2] = new Vector2(u0, 1f);
            uvs[v + 3] = new Vector2(u1, 1f);

            for (int k = 0; k < 4; k++) normals[v + k] = normal;
        }

        private static void Wind(int[] triangles, int t, int v, bool flipped)
        {
            triangles[t] = v;
            triangles[t + 1] = flipped ? v + 1 : v + 2;
            triangles[t + 2] = flipped ? v + 2 : v + 1;
            triangles[t + 3] = v + 1;
            triangles[t + 4] = flipped ? v + 3 : v + 2;
            triangles[t + 5] = flipped ? v + 2 : v + 3;
        }
    }
}
