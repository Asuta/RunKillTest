using UnityEngine;
using System.Collections.Generic;

public class ChineseTowerGenerator : MonoBehaviour
{
    public static void Generate()
    {
        GameObject towerObj = GameObject.Find("ChineseTower");
        if (towerObj != null) DestroyImmediate(towerObj);
        
        towerObj = new GameObject("ChineseTower");
        MeshFilter mf = towerObj.AddComponent<MeshFilter>();
        MeshRenderer mr = towerObj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "PagodaMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Submeshes:
        // 0: Red Wood (Walls, Pillars, Railings)
        // 1: Dark Tiles (Roof, Base)
        // 2: Gold (Accents, Spire)
        List<int> woodTriangles = new List<int>();
        List<int> tileTriangles = new List<int>();
        List<int> goldTriangles = new List<int>();

        int tiers = 7;
        float baseRadius = 3.0f;
        float tierHeight = 2.2f;
        float currentY = 0f;

        // Base Platform (Stone/Tile)
        AddPrism(vertices, normals, uvs, tileTriangles, new Vector3(0, -1.0f, 0), 8, baseRadius + 2.0f, 1.0f); 

        for (int i = 0; i < tiers; i++)
        {
            float ratio = 1f - (float)i * 0.09f; 
            float currentRadius = baseRadius * ratio;
            
            // 1. Walls (Octagonal) with Window details
            AddDetailedWall(vertices, normals, uvs, woodTriangles, new Vector3(0, currentY, 0), 8, currentRadius, tierHeight);
            
            // 2. Pillars at corners (Red)
            AddPillars(vertices, normals, uvs, woodTriangles, new Vector3(0, currentY, 0), 8, currentRadius * 1.05f, tierHeight);

            // 3. Balcony/Railing (Wood)
            if (i > 0)
            {
                float balconyRadius = currentRadius * 1.4f;
                // Floor
                AddPrism(vertices, normals, uvs, woodTriangles, new Vector3(0, currentY, 0), 8, balconyRadius, 0.2f);
                // Railing
                AddRailing(vertices, normals, uvs, woodTriangles, new Vector3(0, currentY + 0.2f, 0), 8, balconyRadius, 0.7f);
                // Support brackets under balcony
                AddBrackets(vertices, normals, uvs, woodTriangles, new Vector3(0, currentY, 0), 8, currentRadius, balconyRadius);
            }

            currentY += tierHeight;

            // 4. Roof (Tiles) with Gold Tips
            float roofWidth = currentRadius * 1.8f;
            float roofHeight = 1.6f;
            AddExquisiteRoof(vertices, normals, uvs, tileTriangles, goldTriangles, new Vector3(0, currentY - 0.3f, 0), 8, roofWidth, roofHeight);
            
            currentY += roofHeight * 0.55f; // Overlap
        }

        // Spire (Gold)
        AddSpire(vertices, normals, uvs, goldTriangles, new Vector3(0, currentY, 0));

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        mesh.subMeshCount = 3;
        mesh.SetTriangles(woodTriangles.ToArray(), 0);
        mesh.SetTriangles(tileTriangles.ToArray(), 1);
        mesh.SetTriangles(goldTriangles.ToArray(), 2);
        
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        // Materials
        Material woodMat = CreateMaterial(new Color(0.6f, 0.1f, 0.1f), "PagodaRed");
        Material tileMat = CreateMaterial(new Color(0.2f, 0.2f, 0.25f), "PagodaTiles");
        Material goldMat = CreateMaterial(new Color(1f, 0.8f, 0.1f), "PagodaGold");
        if (goldMat.HasProperty("_Glossiness")) goldMat.SetFloat("_Glossiness", 0.8f);
        if (goldMat.HasProperty("_Metallic")) goldMat.SetFloat("_Metallic", 0.8f);

        mr.sharedMaterials = new Material[] { woodMat, tileMat, goldMat };
        
        towerObj.transform.position = new Vector3(0, 0, 0);
        
        Debug.Log("Exquisite Chinese Tower Generated!");
    }

    static Material CreateMaterial(Color c, string name)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.shader.name != "Universal Render Pipeline/Lit") m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.name = name;
        return m;
    }

    static void AddDetailedWall(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, int sides, float radius, float height)
    {
        float angleStep = 360f / sides;
        float wallThick = 0.1f;

        for (int i = 0; i < sides; i++)
        {
            float rad1 = i * angleStep * Mathf.Deg2Rad;
            float rad2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(rad1) * radius, 0, Mathf.Sin(rad1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(rad2) * radius, 0, Mathf.Sin(rad2) * radius);
            Vector3 p3 = p2 + Vector3.up * height;
            Vector3 p4 = p1 + Vector3.up * height;

            // Main wall panel
            AddQuad(verts, norms, uvs, tris, p1, p2, p3, p4);

            // Lattice Window (Simple inset box)
            Vector3 faceCenter = (p1 + p2 + p3 + p4) / 4f;
            Vector3 faceNormal = (p1 + p2 - center * 2).normalized; // Approx normal
            faceNormal.y = 0; faceNormal.Normalize();

            float windowW = Vector3.Distance(p1, p2) * 0.6f;
            float windowH = height * 0.6f;
            
            // Add a slightly protruding frame
            AddBoxOriented(verts, norms, uvs, tris, faceCenter + faceNormal * 0.05f, windowW, windowH, 0.1f, Quaternion.LookRotation(faceNormal));
        }
    }

    static void AddRailing(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, int sides, float radius, float height)
    {
        float angleStep = 360f / sides;
        for (int i = 0; i < sides; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
            
            // Post
            AddBox(verts, norms, uvs, tris, pos + Vector3.up * (height/2), 0.15f, height, 0.15f);

            // Handrail to next post
            float radNext = (i + 1) * angleStep * Mathf.Deg2Rad;
            Vector3 nextPos = center + new Vector3(Mathf.Cos(radNext) * radius, 0, Mathf.Sin(radNext) * radius);
            
            Vector3 mid = (pos + nextPos) / 2f + Vector3.up * (height * 0.9f);
            float dist = Vector3.Distance(pos, nextPos);
            Vector3 dir = (nextPos - pos).normalized;
            
            AddBoxOriented(verts, norms, uvs, tris, mid, 0.1f, 0.1f, dist, Quaternion.LookRotation(dir));
            
            // Lower rail
            Vector3 midLow = (pos + nextPos) / 2f + Vector3.up * (height * 0.4f);
            AddBoxOriented(verts, norms, uvs, tris, midLow, 0.08f, 0.08f, dist, Quaternion.LookRotation(dir));
        }
    }

    static void AddBrackets(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, int sides, float innerR, float outerR)
    {
        float angleStep = 360f / sides;
        for (int i = 0; i < sides; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));
            Vector3 start = center + dir * innerR;
            Vector3 end = center + dir * outerR;
            Vector3 mid = (start + end) / 2f + Vector3.down * 0.2f;
            
            // Simple beam
            AddBoxOriented(verts, norms, uvs, tris, mid, 0.15f, 0.15f, (outerR - innerR), Quaternion.LookRotation(dir));
        }
    }

    static void AddExquisiteRoof(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, List<int> goldTris, Vector3 center, int sides, float radius, float height)
    {
        int rings = 8;
        int ringStartIdx = verts.Count;
        float angleStep = 360f / sides;
        
        for (int r = 0; r <= rings; r++)
        {
            float t = (float)r / rings; // 0 to 1
            float currentR = radius * t;
            
            // Curve: y = height * (1 - t^2) but with a flare at the end
            // Base curve
            float y = height * (1f - t);
            // Flare
            float flare = Mathf.Pow(t, 3f) * 0.5f;
            y = height * (1f - t) + flare * 0.5f;
            
            // Adjust to make it concave
            y = height * (1f - Mathf.Sin(t * Mathf.PI * 0.5f)) + (t*t*t)*0.8f;

            for (int s = 0; s < sides; s++)
            {
                float rad = s * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(rad) * currentR;
                float z = Mathf.Sin(rad) * currentR;
                
                // Corner lift (Flying Eaves)
                // Lift more at the corners (where s is integer)
                // Actually s is integer here. We are at a corner.
                // So we just lift the outer rings more.
                
                float cornerLift = 0f;
                if (r > rings / 2)
                {
                    cornerLift = Mathf.Pow((float)(r - rings/2)/(rings/2), 2f) * 0.6f;
                }

                verts.Add(center + new Vector3(x, y + cornerLift, z));
                norms.Add(Vector3.up); 
                uvs.Add(new Vector2((float)s/sides, t));
            }
        }
        
        // Triangulate Roof
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                int current = ringStartIdx + r * sides + s;
                int next = ringStartIdx + r * sides + (s + 1) % sides;
                int above = ringStartIdx + (r + 1) * sides + s;
                int aboveNext = ringStartIdx + (r + 1) * sides + (s + 1) % sides;

                tris.Add(current); tris.Add(above); tris.Add(next);
                tris.Add(next); tris.Add(above); tris.Add(aboveNext);
            }
        }

        // Add Gold Tips at corners
        for (int s = 0; s < sides; s++)
        {
            int lastRingIdx = ringStartIdx + rings * sides + s;
            Vector3 tipPos = verts[lastRingIdx];
            
            // Small gold ornament
            AddBox(verts, norms, uvs, goldTris, tipPos + Vector3.up * 0.1f, 0.2f, 0.2f, 0.2f);
        }
    }

    static void AddSpire(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center)
    {
        AddPrism(verts, norms, uvs, tris, center, 8, 0.3f, 3.0f);
        AddBox(verts, norms, uvs, tris, center + Vector3.up * 1.0f, 0.8f, 0.5f, 0.8f);
        AddBox(verts, norms, uvs, tris, center + Vector3.up * 2.0f, 0.6f, 0.4f, 0.6f);
        AddBox(verts, norms, uvs, tris, center + Vector3.up * 2.8f, 0.4f, 0.3f, 0.4f);
    }

    static void AddPrism(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, int sides, float radius, float height)
    {
        int baseIdx = verts.Count;
        float angleStep = 360f / sides;
        
        for (int i = 0; i < sides; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * radius;
            float z = Mathf.Sin(rad) * radius;
            
            verts.Add(center + new Vector3(x, 0, z)); 
            verts.Add(center + new Vector3(x, height, z)); 
            
            norms.Add(new Vector3(x, 0, z).normalized);
            norms.Add(new Vector3(x, 0, z).normalized);
            
            uvs.Add(new Vector2((float)i / sides, 0));
            uvs.Add(new Vector2((float)i / sides, 1));
        }

        for (int i = 0; i < sides; i++)
        {
            int current = baseIdx + 2 * i;
            int next = baseIdx + 2 * ((i + 1) % sides);
            int b0 = current, t0 = current + 1, b1 = next, t1 = next + 1;

            tris.Add(b0); tris.Add(t0); tris.Add(b1);
            tris.Add(t0); tris.Add(t1); tris.Add(b1);
        }
        
        // Top Cap
        int centerIdx = verts.Count;
        verts.Add(center + new Vector3(0, height, 0));
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));
        
        for (int i = 0; i < sides; i++)
        {
            int t0 = baseIdx + 2 * i + 1;
            int t1 = baseIdx + 2 * ((i + 1) % sides) + 1;
            tris.Add(centerIdx); tris.Add(t1); tris.Add(t0);
        }
    }

    static void AddPillars(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, int sides, float radius, float height)
    {
        float angleStep = 360f / sides;
        for (int i = 0; i < sides; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
            AddBox(verts, norms, uvs, tris, pos + Vector3.up * (height/2), 0.2f, height, 0.2f);
        }
    }

    static void AddBox(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, float width, float height, float depth)
    {
        AddBoxOriented(verts, norms, uvs, tris, center, width, height, depth, Quaternion.identity);
    }

    static void AddBoxOriented(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, float width, float height, float depth, Quaternion rotation)
    {
        Vector3[] points = new Vector3[8];
        Vector3 extents = new Vector3(width/2, height/2, depth/2);
        
        points[0] = center + rotation * new Vector3(-extents.x, -extents.y, -extents.z);
        points[1] = center + rotation * new Vector3( extents.x, -extents.y, -extents.z);
        points[2] = center + rotation * new Vector3( extents.x, -extents.y,  extents.z);
        points[3] = center + rotation * new Vector3(-extents.x, -extents.y,  extents.z);
        points[4] = center + rotation * new Vector3(-extents.x,  extents.y, -extents.z);
        points[5] = center + rotation * new Vector3( extents.x,  extents.y, -extents.z);
        points[6] = center + rotation * new Vector3( extents.x,  extents.y,  extents.z);
        points[7] = center + rotation * new Vector3(-extents.x,  extents.y,  extents.z);

        int baseIndex = verts.Count;
        
        // Add vertices
        foreach(var p in points) { verts.Add(p); norms.Add(Vector3.up); uvs.Add(Vector2.zero); } // Simplified norms/uvs for box

        // Front (0,1,5,4)
        AddQuadIndices(tris, baseIndex, 0, 1, 5, 4);
        // Back (2,3,7,6)
        AddQuadIndices(tris, baseIndex, 2, 3, 7, 6);
        // Left (3,0,4,7)
        AddQuadIndices(tris, baseIndex, 3, 0, 4, 7);
        // Right (1,2,6,5)
        AddQuadIndices(tris, baseIndex, 1, 2, 6, 5);
        // Top (4,5,6,7)
        AddQuadIndices(tris, baseIndex, 4, 5, 6, 7);
        // Bottom (3,2,1,0)
        AddQuadIndices(tris, baseIndex, 3, 2, 1, 0);
    }

    static void AddQuadIndices(List<int> tris, int baseIdx, int i0, int i1, int i2, int i3)
    {
        tris.Add(baseIdx + i0); tris.Add(baseIdx + i2); tris.Add(baseIdx + i1);
        tris.Add(baseIdx + i0); tris.Add(baseIdx + i3); tris.Add(baseIdx + i2);
    }

    static void AddQuad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        int idx = verts.Count;
        verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
        
        Vector3 n = Vector3.Cross(p1 - p0, p3 - p0).normalized;
        norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
        
        uvs.Add(new Vector2(0,0)); uvs.Add(new Vector2(1,0)); uvs.Add(new Vector2(1,1)); uvs.Add(new Vector2(0,1));
        
        tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 1);
        tris.Add(idx); tris.Add(idx + 3); tris.Add(idx + 2);
    }
}
