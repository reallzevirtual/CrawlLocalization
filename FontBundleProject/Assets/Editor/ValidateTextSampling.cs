using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Offline visual/geometry check in the matching editor; never starts Crawl.
public static class ValidateTextSampling
{
    public static void Run()
    {
        string root = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string output = Path.Combine(root, "artifacts/text-sampling");
        Directory.CreateDirectory(output);
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(root, "Fonts/crawl-fonts.bundle"));
        if (bundle == null) throw new Exception("Font bundle not loaded");
        Font font = bundle.LoadAsset<Font>("assets/fonts/crawltextbodythin.ttf");
        GameObject cameraObject = new GameObject("SamplingCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 25;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1);
        RenderTexture target = new RenderTexture(900, 450, 24);
        camera.targetTexture = target;
        string[] texts = { "A slower\npace to\nlearn the\nropes", "节奏较慢\n适合熟悉\n游戏玩法", "节奏较慢\n适合熟悉\n游戏玩法" };
        TextMesh[] meshes = new TextMesh[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject obj = new GameObject("Sample" + i);
            obj.transform.position = new Vector3((i - 1) * 32, 10, 0);
            TextMesh mesh = obj.AddComponent<TextMesh>();
            mesh.font = font;
            mesh.fontSize = i == 2 ? 160 : 16;
            mesh.characterSize = i == 2 ? 1 : 10;
            mesh.lineSpacing = 0.42f;
            mesh.anchor = TextAnchor.UpperCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            mesh.GetComponent<Renderer>().sharedMaterial = font.material;
            mesh.text = texts[i];
            meshes[i] = mesh;
        }
        font.RequestCharactersInTexture(texts[0] + texts[1], 16, FontStyle.Normal);
        font.RequestCharactersInTexture(texts[2], 160, FontStyle.Normal);
        font.material.mainTexture.filterMode = FilterMode.Point;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        File.WriteAllBytes(Path.Combine(output, "sampling-comparison.png"), image.EncodeToPNG());
        string report = "Left: original English 16/10; middle: Chinese 16/10; right: Chinese 160/1\n";
        for (int i = 0; i < meshes.Length; i++)
        {
            Bounds bounds = meshes[i].GetComponent<Renderer>().bounds;
            report += "Sample " + i + ": size=" + bounds.size.ToString("F4") + "; local center=" + (bounds.center - meshes[i].transform.position).ToString("F4") + "\n";
        }
        Bounds low = meshes[1].GetComponent<Renderer>().bounds;
        Bounds high = meshes[2].GetComponent<Renderer>().bounds;
        if (Mathf.Abs(low.size.y - high.size.y) > 0.001f || Mathf.Abs(low.center.y - high.center.y) > 0.001f)
            throw new Exception("Sampling correction changed the label's vertical layout: " + report);
        File.WriteAllText(Path.Combine(output, "geometry.txt"), report);
        Debug.Log("CRAWL_TEXT_SAMPLING_OK\n" + report);
        for (int i = 0; i < meshes.Length; i++) UnityEngine.Object.DestroyImmediate(meshes[i].gameObject);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
        bundle.Unload(true);
    }
}
