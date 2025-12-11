using UnityEngine;
using UnityEditor;

public class TowerBuilder
{
    public static void BuildTower()
    {
        string rootName = "ChineseTower";
        GameObject existing = GameObject.Find(rootName);
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing);
        }

        GameObject root = new GameObject(rootName);
        root.transform.position = Vector3.zero;

        // Load Materials
        Material matRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tower/Tower_Red.mat");
        Material matRoof = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tower/Tower_Roof.mat");
        Material matBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tower/Tower_Base.mat");
        Material matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tower/Tower_Gold.mat");

        if (matRed == null) Debug.LogError("MatRed not found");

        float currentHeight = 0;

        // --- Foundation ---
        CreateCube(root.transform, "Foundation", new Vector3(0, 0.5f, 0), new Vector3(14, 1, 14), matBase);
        currentHeight += 1.0f;

        // --- Levels ---
        int levels = 3;
        float[] bodyWidths = { 8f, 6f, 4f };
        float[] heights = { 4f, 3.5f, 3f };
        float[] roofWidths = { 12f, 9f, 6f }; // The main roof overhang
        
        for (int i = 0; i < levels; i++)
        {
            float w = bodyWidths[i];
            float h = heights[i];
            float rw = roofWidths[i];

            // 1. Floor Base / Balcony (slightly wider than body)
            CreateCube(root.transform, $"Level{i}_Floor", new Vector3(0, currentHeight + 0.25f, 0), new Vector3(w + 2, 0.5f, w + 2), matBase);
            
            // Railings (Simple blocks at corners and edges)
            CreateRailings(root.transform, currentHeight + 0.5f, w + 2, matRed);

            currentHeight += 0.5f;

            // 2. Body (Walls)
            CreateCube(root.transform, $"Level{i}_Body", new Vector3(0, currentHeight + h/2, 0), new Vector3(w, h, w), matRed);

            // Columns (Corners)
            float colOffset = w / 2 - 0.25f;
            float colSize = 0.8f; // Slightly popping out
            CreateCube(root.transform, $"Level{i}_Col_FL", new Vector3(colOffset, currentHeight + h/2, colOffset), new Vector3(colSize, h, colSize), matRed);
            CreateCube(root.transform, $"Level{i}_Col_FR", new Vector3(colOffset, currentHeight + h/2, -colOffset), new Vector3(colSize, h, colSize), matRed);
            CreateCube(root.transform, $"Level{i}_Col_BL", new Vector3(-colOffset, currentHeight + h/2, colOffset), new Vector3(colSize, h, colSize), matRed);
            CreateCube(root.transform, $"Level{i}_Col_BR", new Vector3(-colOffset, currentHeight + h/2, -colOffset), new Vector3(colSize, h, colSize), matRed);

            // Windows/Doors (Simple recessed black or gold blocks? Let's use Gold for decoration)
            // Door on Level 0
            if (i == 0)
            {
                CreateCube(root.transform, "Door", new Vector3(0, currentHeight + 1.5f, -w/2 - 0.1f), new Vector3(2, 3, 0.5f), matGold);
            }
            else
            {
                // Windows
                CreateCube(root.transform, $"Level{i}_Win_F", new Vector3(0, currentHeight + h/2, -w/2 - 0.1f), new Vector3(1.5f, 1.5f, 0.2f), matGold);
                CreateCube(root.transform, $"Level{i}_Win_B", new Vector3(0, currentHeight + h/2, w/2 + 0.1f), new Vector3(1.5f, 1.5f, 0.2f), matGold);
                CreateCube(root.transform, $"Level{i}_Win_L", new Vector3(-w/2 - 0.1f, currentHeight + h/2, 0), new Vector3(0.2f, 1.5f, 1.5f), matGold);
                CreateCube(root.transform, $"Level{i}_Win_R", new Vector3(w/2 + 0.1f, currentHeight + h/2, 0), new Vector3(0.2f, 1.5f, 1.5f), matGold);
            }

            currentHeight += h;

            // 3. Roof
            // Main overhang
            CreateCube(root.transform, $"Level{i}_Roof_Main", new Vector3(0, currentHeight + 0.25f, 0), new Vector3(rw, 0.5f, rw), matRoof);
            
            // Roof Step up (Pyramid effect approximation)
            CreateCube(root.transform, $"Level{i}_Roof_Top", new Vector3(0, currentHeight + 0.75f, 0), new Vector3(rw * 0.7f, 0.5f, rw * 0.7f), matRoof);

            // Eave corners (upturn effect - simple blocks at corners)
            float cornerDist = rw / 2 - 0.5f;
            CreateCube(root.transform, $"Level{i}_Eave_FL", new Vector3(cornerDist, currentHeight + 0.5f, cornerDist), new Vector3(1.5f, 0.5f, 1.5f), matRoof);
            CreateCube(root.transform, $"Level{i}_Eave_FR", new Vector3(cornerDist, currentHeight + 0.5f, -cornerDist), new Vector3(1.5f, 0.5f, 1.5f), matRoof);
            CreateCube(root.transform, $"Level{i}_Eave_BL", new Vector3(-cornerDist, currentHeight + 0.5f, cornerDist), new Vector3(1.5f, 0.5f, 1.5f), matRoof);
            CreateCube(root.transform, $"Level{i}_Eave_BR", new Vector3(-cornerDist, currentHeight + 0.5f, -cornerDist), new Vector3(1.5f, 0.5f, 1.5f), matRoof);

            currentHeight += 1.0f; // Roof height
        }

        // --- Spire ---
        CreateCube(root.transform, "Spire_Base", new Vector3(0, currentHeight + 0.5f, 0), new Vector3(2, 1, 2), matRoof);
        currentHeight += 1.0f;
        CreateCube(root.transform, "Spire_Mid", new Vector3(0, currentHeight + 1.0f, 0), new Vector3(1, 2, 1), matGold);
        currentHeight += 2.0f;
        CreateCube(root.transform, "Spire_Top", new Vector3(0, currentHeight + 0.5f, 0), new Vector3(0.5f, 1, 0.5f), matGold);

        // Select the object
        Selection.activeGameObject = root;
    }

    private static void CreateCube(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        
        if (mat != null)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
    }

    private static void CreateRailings(Transform parent, float yPos, float width, Material mat)
    {
        float thickness = 0.2f;
        float height = 0.6f;
        float offset = width / 2 - thickness / 2;

        // 4 sides
        CreateCube(parent, "Rail_F", new Vector3(0, yPos + height/2, offset), new Vector3(width, height, thickness), mat);
        CreateCube(parent, "Rail_B", new Vector3(0, yPos + height/2, -offset), new Vector3(width, height, thickness), mat);
        CreateCube(parent, "Rail_L", new Vector3(-offset, yPos + height/2, 0), new Vector3(thickness, height, width), mat);
        CreateCube(parent, "Rail_R", new Vector3(offset, yPos + height/2, 0), new Vector3(thickness, height, width), mat);
    }
}
