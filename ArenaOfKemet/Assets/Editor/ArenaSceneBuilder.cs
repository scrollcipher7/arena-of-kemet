#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-click map builder: menu Arena -> Build 3-Lane Map.
// Raises the whole Honor-of-Kings-style field — three lanes, river, jungle,
// 18 towers (three deep on every road), 2 cores, both heroes, camera and the GameManager — in one press.
public static class ArenaSceneBuilder
{
    // (Lane geometry lives in GameManager.Lanes — the Gorge's three roads.)
    [MenuItem("Arena/Build 3-Lane Map")]
    public static void Build()
    {
        // The map cannot be reborn while the war is running — Unity
        // forbids it and throws. Meet the click with a sign, not a crash.
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("THE ARENA IS ALIVE",
                "Stop Play first (the blue triangle up top), then build. The war must end before the land can be remade.", "Understood");
            return;
        }
        // Clear the previous build and any hand-made field pieces.
        var old = GameObject.Find("ARENA_ROOT");
        if (old != null) Object.DestroyImmediate(old);
        foreach (string n in new[] { "Ground", "Lane", "River" })
        {
            var g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var sandDark = GetMat("ArenaSand", "6B543A");
        // (The old lane-stone and river-blue paints retired the day the
        // earth was painted whole — their .mat files rest in Materials.)
        var goldKemet = GetMat("ArenaGold", "D9A41F");
        var stoneOly = GetMat("ArenaOlympus", "8FA6B8");
        var trunk = GetMat("ArenaTrunk", "4A3521");
        var leaf = GetMat("ArenaLeaf", "2E4A22");
        var bush = GetMat("ArenaBush", "3A5C2A");
        var rockMat = GetMat("ArenaRock", "6E6A61");
        var scarabMat = GetMat("ArenaScarab", "B5892E");
        var crocMat = GetMat("ArenaCroc", "2F5233");
        var apophisMat = GetMat("ArenaApophis", "241430");
        var sphinxMat = GetMat("ArenaSphinx", "C8A86B");
        var pitMat = GetMat("ArenaPit", "3A3230");

        // The two home cores render from EVERY side — no angle can ever
        // make the pyramid or the temple vanish or turn inside-out.
        goldKemet.SetFloat("_Cull", 0f); EditorUtility.SetDirty(goldKemet);
        stoneOly.SetFloat("_Cull", 0f); EditorUtility.SetDirty(stoneOly);

        var root = new GameObject("ARENA_ROOT").transform;

        // THE PAINTED EARTH: one whole skin for the Gorge — the roads,
        // the river, the plazas and the lairs drawn INTO the ground
        // itself (GroundPaint.jpg in Assets/Materials), no floating
        // strips, no pasted-on water. If the painting is missing, the
        // honest sand stands in its place.
        string[] laneName = { "Top", "Mid", "Bot" };
        Prim(PrimitiveType.Plane, "Ground", root, new Vector3(0, 0, 0), new Vector3(12, 1, 12),
            PaintedGroundMaterial(sandDark));

        // Base pads in the corners: Kemet southwest, Olympus northeast.
        Prim(PrimitiveType.Cube, "KemetBase", root, new Vector3(-50, 0.05f, -50), new Vector3(17, 0.1f, 17), goldKemet);
        Prim(PrimitiveType.Cube, "OlympusBase", root, new Vector3(50, 0.05f, 50), new Vector3(17, 0.1f, 17), stoneOly);

        // Towers stand THREE DEEP on every road, as in the maker's
        // maps — and each court keeps to its OWN half of the march:
        // Gate at 10%, Inner at 22%, Outer at 38%. (An earlier build
        // measured the Outer against the WHOLE road and planted the
        // two courts' front towers in each other's land — the maker's
        // eye caught it from above. Never again.)
        for (int i = 0; i < 3; i++)
        {
            var path = GameManager.Lanes[i];
            Vector3 ko = PathPoint(path, 0.38f), ki = PathPoint(path, 0.22f), kg = PathPoint(path, 0.10f);
            Vector3 oo = PathPoint(path, 0.62f), oi = PathPoint(path, 0.78f), og = PathPoint(path, 0.90f);
            BuildStructure(root, "KemetTower_" + laneName[i] + "_Outer", Team.Kemet, false, ko.x, ko.z, goldKemet);
            BuildStructure(root, "KemetTower_" + laneName[i] + "_Inner", Team.Kemet, false, ki.x, ki.z, goldKemet);
            BuildStructure(root, "KemetTower_" + laneName[i] + "_Gate", Team.Kemet, false, kg.x, kg.z, goldKemet);
            BuildStructure(root, "OlympusTower_" + laneName[i] + "_Outer", Team.Olympus, false, oo.x, oo.z, stoneOly);
            BuildStructure(root, "OlympusTower_" + laneName[i] + "_Inner", Team.Olympus, false, oi.x, oi.z, stoneOly);
            BuildStructure(root, "OlympusTower_" + laneName[i] + "_Gate", Team.Olympus, false, og.x, og.z, stoneOly);
        }
        BuildPyramidCore(root, "TempleCore", -50, -50, goldKemet);
        BuildGreekTempleCore(root, "ShrineCore", 50, 50, stoneOly);

        // THE TEN GODS take their seats — five against five.
        // Kemet (home SW): Medjay gold road (Bot), Thoth mid, Sekhmet clash
        // road (Top), Anubis the jungle, Isis roaming. Olympus mirrored
        // through the center: Artemis gold (Top), Zeus mid, Ares clash (Bot),
        // Hades the jungle, Athena roaming.
        BuildHero(root, "Medjay", "THE MEDJAY", Team.Kemet, true,
            HeroAI.Laner, 2, -43, -43, 6.5f, goldKemet, 0.92f, 1.10f, 1.0f);
        BuildHero(root, "Thoth", "THOTH", Team.Kemet, false,
            HeroAI.Laner, 1, -41, -45, 6.0f, goldKemet, 0.95f, 1.05f, 1.0f);
        BuildHero(root, "Sekhmet", "SEKHMET", Team.Kemet, false,
            HeroAI.Laner, 0, -45, -41, 3.4f, goldKemet, 1.35f, 0.95f, 0.98f);
        BuildHero(root, "Anubis", "ANUBIS", Team.Kemet, false,
            HeroAI.Jungler, 1, -47, -43, 3.2f, goldKemet, 1.0f, 1.15f, 1.08f);
        BuildHero(root, "Isis", "ISIS", Team.Kemet, false,
            HeroAI.Support, 1, -43, -47, 5.5f, goldKemet, 1.05f, 0.9f, 1.02f);
        BuildHero(root, "Ares", "ARES", Team.Olympus, false,
            HeroAI.Laner, 2, 43, 43, 3.4f, stoneOly, 1.3f, 1.0f, 0.98f);
        BuildHero(root, "Artemis", "ARTEMIS", Team.Olympus, false,
            HeroAI.Laner, 0, 45, 41, 6.5f, stoneOly, 0.92f, 1.10f, 1.0f);
        BuildHero(root, "Zeus", "ZEUS", Team.Olympus, false,
            HeroAI.Laner, 1, 41, 45, 6.0f, stoneOly, 0.95f, 1.05f, 1.0f);
        BuildHero(root, "Hades", "HADES", Team.Olympus, false,
            HeroAI.Jungler, 1, 47, 43, 3.2f, stoneOly, 1.0f, 1.15f, 1.08f);
        BuildHero(root, "Athena", "ATHENA", Team.Olympus, false,
            HeroAI.Support, 1, 43, 47, 5.5f, stoneOly, 1.2f, 0.95f, 1.0f);

        // Jungle camps in the pockets between the roads, mirrored through
        // the center so neither side's jungle is richer.
        BuildScarab(root, "ScarabPack_NorthWest", -26, 8, scarabMat);
        BuildScarab(root, "ScarabPack_SouthEast", 26, -8, scarabMat);
        BuildScarab(root, "ScarabPack_SouthWest", -8, -26, scarabMat);
        BuildScarab(root, "ScarabPack_NorthEast", 8, 26, scarabMat);
        BuildCroc(root, "SobekCroc_NorthPool", -16, 16, crocMat);
        BuildCroc(root, "SobekCroc_SouthPool", 16, -16, crocMat);

        // The two great bosses, each in its own jungle home by the pools.
        // They sleep until a hero reaches level 5. Fell one, summon it (T).
        BuildApophis(root, "APOPHIS", -27, 12, apophisMat, pitMat);
        BuildSphinx(root, "THE SPHINX", 27, -12, sphinxMat, pitMat);

        // The director.
        var gm = new GameObject("GameManager");
        gm.transform.SetParent(root);
        gm.AddComponent<GameManager>();
        gm.AddComponent<HUD>();
        gm.AddComponent<HudStage>();
        gm.AddComponent<Minimap>();
        gm.AddComponent<AudioDirector>();
        gm.AddComponent<IntroScreen>();
        gm.AddComponent<SelectionHall>();
        gm.AddComponent<FogOfWar>();

        // Camera over the whole war — and the rig that rides with your god.
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 68, -50);
            cam.transform.rotation = Quaternion.Euler(56, 0, 0);
            if (cam.GetComponent<FollowCamera>() == null)
                cam.gameObject.AddComponent<FollowCamera>();
        }
        BuildPostFX();
        DressSun();

        // The jungle fills the pockets between the roads: trees, brush, rocks.
        // Spots are rejected on roads, in the river, and at the homes/camps.
        var rng = new System.Random(7);
        int placed = 0, guard = 0;
        while (placed < 48 && guard++ < 500)
        {
            float bx = (float)(rng.NextDouble() * 108 - 54);
            float bz = (float)(rng.NextDouble() * 108 - 54);
            if (!JungleSpot(new Vector3(bx, 0, bz))) continue;
            Prim(PrimitiveType.Cylinder, "JungleTree", root, new Vector3(bx, 0.6f, bz), new Vector3(0.35f, 1.2f, 0.35f), trunk);
            Prim(PrimitiveType.Sphere, "JungleCanopy", root, new Vector3(bx, 2.1f, bz), new Vector3(2.3f, 1.7f, 2.3f), leaf);
            placed++;
        }
        placed = 0; guard = 0;
        while (placed < 22 && guard++ < 400)
        {
            float bx = (float)(rng.NextDouble() * 108 - 54);
            float bz = (float)(rng.NextDouble() * 108 - 54);
            if (!JungleSpot(new Vector3(bx, 0, bz))) continue;
            Prim(PrimitiveType.Sphere, "JungleBush", root, new Vector3(bx, 0.3f, bz), new Vector3(1.9f, 0.7f, 1.9f), bush);
            placed++;
        }
        placed = 0; guard = 0;
        while (placed < 16 && guard++ < 400)
        {
            float bx = (float)(rng.NextDouble() * 108 - 54);
            float bz = (float)(rng.NextDouble() * 108 - 54);
            if (!JungleSpot(new Vector3(bx, 0, bz))) continue;
            var rock = Prim(PrimitiveType.Cube, "JungleRock", root, new Vector3(bx, 0.5f, bz),
                new Vector3(2.2f + (float)rng.NextDouble() * 1.6f, 1.2f, 1.8f), rockMat);
            rock.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 180f, 0);
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // SELF-CHECK: the build proves itself before it asks to be saved.
        var structs = Object.FindObjectsByType<Structure>(FindObjectsInactive.Include);
        int cores = 0, towers = 0;
        Vector3 cK = Vector3.zero, cO = Vector3.zero;
        foreach (var s in structs)
        {
            if (s.isCore) { cores++; if (s.team == Team.Kemet) cK = s.transform.position; else cO = s.transform.position; }
            else towers++;
        }
        bool coreDiag = Vector3.Distance(cK, cO) > 130f;
        int beastsN = Object.FindObjectsByType<JungleBeast>(FindObjectsInactive.Include).Length;
        int heroesN = Object.FindObjectsByType<Hero>(FindObjectsInactive.Include).Length;
        int groundN = 0;
        foreach (Transform ch in root)
            if (ch.name == "Ground") groundN++;
        bool pass = cores == 2 && coreDiag && towers == 18 && beastsN == 8
            && heroesN == 10 && groundN == 1;
        string report = "cores " + cores + " (diagonal " + (coreDiag ? "OK" : "WRONG") + ")"
            + ", towers " + towers + "/18, beasts " + beastsN + "/8"
            + ", heroes " + heroesN + "/10, painted earth " + (groundN == 1 ? "whole" : "MISSING");
        Debug.Log("ARENA SELF-CHECK " + (pass ? "PASSED: " : "FAILED: ") + report);
        EditorUtility.DisplayDialog("Arena of Kemet — Self-Check",
            (pass ? "THE GORGE STANDS TRUE.\n" : "SOMETHING IS WRONG — send me a picture of this box:\n")
            + report
            + "\n\nPyramid SW, Greek Temple NE, three bent roads, diagonal river. TEN GODS take the field: five of Kemet, five of Olympus, each at his post. Jungle camps wake at 0:30; the Ancients sleep until level 5."
            + "\n\nSave with Ctrl+S, then press Play.", "Understood");
    }

    static float SegDist(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a; float len2 = ab.sqrMagnitude;
        float t = len2 > 0 ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2) : 0f;
        return Vector3.Distance(p, a + ab * t);
    }

    static Vector3 PathPoint(Vector3[] path, float t)
    {
        float total = 0;
        for (int i = 0; i < path.Length - 1; i++) total += Vector3.Distance(path[i], path[i + 1]);
        float want = total * Mathf.Clamp01(t);
        for (int i = 0; i < path.Length - 1; i++)
        {
            float len = Vector3.Distance(path[i], path[i + 1]);
            if (want <= len) return Vector3.Lerp(path[i], path[i + 1], len > 0 ? want / len : 0f);
            want -= len;
        }
        return path[path.Length - 1];
    }

    static bool JungleSpot(Vector3 p)
    {
        for (int li = 0; li < 3; li++)
        {
            var path = GameManager.Lanes[li];
            for (int i = 0; i < path.Length - 1; i++)
                if (SegDist(p, path[i], path[i + 1]) < 5.5f) return false;
        }
        if (Vector3.Distance(p, new Vector3(-24, 0, 24)) < 15f) return false;   // pools
        if (Vector3.Distance(p, new Vector3(24, 0, -24)) < 15f) return false;
        if (Mathf.Abs(p.x + p.z) / 1.414f < 5.5f) return false;                 // channel
        if (Vector3.Distance(p, new Vector3(-50, 0, -50)) < 11f) return false;  // homes
        if (Vector3.Distance(p, new Vector3(50, 0, 50)) < 11f) return false;
        return true;
    }

    static Material PaintedGroundMaterial(Material fallback)
    {
        string texPath = "Assets/Materials/GroundPaint.jpg";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) return fallback;
        var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null && imp.maxTextureSize < 2048)
        {
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }
        string path = "Assets/Materials/ArenaGroundPaint.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_BaseMap", tex);
        m.color = Color.white;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material GetMat(string name, string hex)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        if (ColorUtility.TryParseHtmlString("#" + hex, out var c)) m.color = c;
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent,
        Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<Renderer>().material = mat;
        return go;
    }

    static void BuildScarab(Transform root, string name, float x, float z, Material mat)
    {
        var go = Prim(PrimitiveType.Sphere, name, root,
            new Vector3(x, 0.8f, z), new Vector3(1.7f, 1.3f, 1.7f), mat);
        Prim(PrimitiveType.Sphere, name + "_a", go.transform,
            new Vector3(x - 1.7f, 0.45f, z + 1.0f), new Vector3(0.9f, 0.7f, 0.9f), mat);
        Prim(PrimitiveType.Sphere, name + "_b", go.transform,
            new Vector3(x + 1.6f, 0.45f, z - 1.1f), new Vector3(0.9f, 0.7f, 0.9f), mat);
        var b = go.AddComponent<JungleBeast>();
        b.beastName = "Scarab Pack";
        b.damage = 14f; b.goldReward = 50; b.xpReward = 60f; b.respawnSeconds = 70f;
        b.health = go.AddComponent<Health>();
        b.health.Init(550f);
        go.SetActive(false);   // camps wake at 0:30, as the reference commands
    }

    static void BuildCroc(Transform root, string name, float x, float z, Material mat)
    {
        var go = Prim(PrimitiveType.Sphere, name, root,
            new Vector3(x, 0.55f, z), new Vector3(1.5f, 1.05f, 3.4f), mat);
        var b = go.AddComponent<JungleBeast>();
        b.beastName = "Sobek's Croc";
        b.damage = 30f; b.attackRange = 4.2f;
        b.goldReward = 120; b.xpReward = 130f;
        b.grantsBuff = true; b.respawnSeconds = 90f;
        b.health = go.AddComponent<Health>();
        b.health.Init(1500f);
        go.SetActive(false);   // camps wake at 0:30, as the reference commands
    }

    static void BuildApophis(Transform root, string name, float x, float z, Material mat, Material pit)
    {
        Prim(PrimitiveType.Cylinder, name + "_Pit", root,
            new Vector3(x, 0.09f, z), new Vector3(8f, 0.18f, 8f), pit);
        var go = Prim(PrimitiveType.Sphere, name, root,
            new Vector3(x, 0.9f, z), new Vector3(2.0f, 1.5f, 5.8f), mat);
        var b = go.AddComponent<JungleBeast>();
        b.beastName = "APOPHIS, the Chaos Serpent";
        b.isBoss = true; b.bossKind = "Apophis";
        b.damage = 45f; b.attackRange = 4.6f; b.aggroRange = 9.5f; b.leashRange = 13f;
        b.slamDamage = 90f;
        b.goldReward = 250; b.xpReward = 250f; b.respawnSeconds = 150f;
        b.health = go.AddComponent<Health>();
        b.health.Init(4200f);
    }

    static void BuildSphinx(Transform root, string name, float x, float z, Material mat, Material pit)
    {
        Prim(PrimitiveType.Cylinder, name + "_Pit", root,
            new Vector3(x, 0.09f, z), new Vector3(8f, 0.18f, 8f), pit);
        var go = Prim(PrimitiveType.Cube, name, root,
            new Vector3(x, 1.4f, z), new Vector3(3.4f, 2.8f, 4.4f), mat);
        Prim(PrimitiveType.Sphere, name + "_Head", go.transform,
            new Vector3(x, 3.4f, z), new Vector3(1.7f, 1.7f, 1.7f), mat);
        var b = go.AddComponent<JungleBeast>();
        b.beastName = "THE SPHINX, Stone Guardian";
        b.isBoss = true; b.bossKind = "Sphinx";
        b.damage = 45f; b.attackRange = 4.6f; b.aggroRange = 9.5f; b.leashRange = 13f;
        b.slamDamage = 90f;
        b.goldReward = 250; b.xpReward = 250f; b.respawnSeconds = 150f;
        b.health = go.AddComponent<Health>();
        b.health.Init(4200f);
    }

    static Mesh PyramidMesh(float w, float h)
    {
        float a = w / 2f;
        var verts = new Vector3[]
        {
            new Vector3(-a, 0, -a), new Vector3(a, 0, -a),
            new Vector3(a, 0, a), new Vector3(-a, 0, a),
            new Vector3(0, h, 0)
        };
        var tris = new int[]
        {
            0, 4, 1,  1, 4, 2,  2, 4, 3,  3, 4, 0,   // four faces
            0, 1, 2,  0, 2, 3                        // base
        };
        var m = new Mesh();
        m.vertices = verts;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static void BuildPyramidCore(Transform root, string name, float x, float z, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = new Vector3(x, 0, z);
        go.transform.rotation = Quaternion.Euler(0, 45f, 0);   // face the diagonal war
        go.AddComponent<MeshFilter>().mesh = PyramidMesh(11f, 8f);
        go.AddComponent<MeshRenderer>().material = mat;
        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(11f, 8f, 11f);
        bc.center = new Vector3(0, 4f, 0);
        // The golden capstone — the pyramidion that catches the sun.
        var cap = Prim(PrimitiveType.Sphere, name + "_Capstone", go.transform,
            new Vector3(x, 8.3f, z), new Vector3(1.1f, 1.1f, 1.1f), mat);
        Object.DestroyImmediate(cap.GetComponent<Collider>());
        // The Vision's crown: a great crystal burning over the pyramid.
        CrystalCluster(go.transform, new Vector3(x, 9.7f, z), 1.6f, Vector3.one);
        var s = go.AddComponent<Structure>();
        s.team = Team.Kemet; s.isCore = true; s.radius = 4.2f;
        var pbar = go.AddComponent<OverheadBar>();
        pbar.height = 13.4f; pbar.barScale = 4.6f;
    }

    static void BuildGreekTempleCore(Transform root, string name, float x, float z, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = new Vector3(x, 0, z);
        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(11f, 7f, 10f);
        bc.center = new Vector3(0, 3.5f, 0);
        Transform t = go.transform;
        // Platform, columns, roof slab, pediment ridge.
        var plat = Prim(PrimitiveType.Cube, name + "_Platform", t,
            new Vector3(x, 0.5f, z), new Vector3(11f, 1f, 9f), mat);
        Object.DestroyImmediate(plat.GetComponent<Collider>());
        for (int i = 0; i < 4; i++)
        {
            float cx = x - 4.5f + i * 3f;
            foreach (float cz in new[] { z - 3.2f, z + 3.2f })
            {
                var col = Prim(PrimitiveType.Cylinder, name + "_Column", t,
                    new Vector3(cx, 3f, cz), new Vector3(0.9f, 4f, 0.9f), mat);
                Object.DestroyImmediate(col.GetComponent<Collider>());
            }
        }
        var roof = Prim(PrimitiveType.Cube, name + "_Roof", t,
            new Vector3(x, 5.4f, z), new Vector3(12f, 0.8f, 10f), mat);
        Object.DestroyImmediate(roof.GetComponent<Collider>());
        var ped = Prim(PrimitiveType.Cube, name + "_Pediment", t,
            new Vector3(x, 6.3f, z), new Vector3(11.5f, 2.4f, 2.4f), mat);
        ped.transform.rotation = Quaternion.Euler(45f, 0, 0);
        Object.DestroyImmediate(ped.GetComponent<Collider>());
        // Olympus answers with a crystal of its own over the pediment.
        CrystalCluster(go.transform, new Vector3(x, 8.15f, z), 1.35f, Vector3.one);
        var s = go.AddComponent<Structure>();
        s.team = Team.Olympus; s.isCore = true; s.radius = 4.4f;
        var gbar = go.AddComponent<OverheadBar>();
        gbar.height = 11.9f; gbar.barScale = 4.6f;
    }

    static void BuildHero(Transform root, string objName, string heroName, Team team,
        bool isPlayer, int role, int homeLane, float x, float z, float range,
        Material mat, float hpM, float dmgM, float spdM)
    {
        var go = Prim(PrimitiveType.Capsule, objName, root, new Vector3(x, 1, z), Vector3.one, mat);
        var h = go.AddComponent<Hero>();
        h.isPlayer = isPlayer; h.heroName = heroName; h.team = team; h.attackRange = range;
        h.roleHpMult = hpM; h.roleDmgMult = dmgM; h.roleSpdMult = spdM;
        if (!isPlayer)
        {
            var ai = go.AddComponent<HeroAI>();
            ai.role = role; ai.homeLane = homeLane;
            // Each god's born temperament — the mood he wakes with,
            // before any match turns him (HeroAI re-rolls some).
            switch (heroName)
            {
                case "SEKHMET": case "ARES": case "ATHENA": ai.temperament = HeroAI.Push; break;
                case "THOTH": case "ISIS": case "ARTEMIS": ai.temperament = HeroAI.Farm; break;
                case "ANUBIS": case "HADES": ai.temperament = HeroAI.Hunt; break;
                case "ZEUS": ai.temperament = HeroAI.Split; break;
            }
        }
        var bar = go.AddComponent<OverheadBar>();
        bar.height = 2.9f; bar.showText = true;
        DressBody(go, heroName);
    }

    static readonly string[] AllProps =
    {
        "1H_Axe", "1H_Axe_Offhand", "2H_Axe", "Barbarian_Round_Shield", "Barbarian_Hat", "Barbarian_Cape", "Mug",
        "1H_Sword", "1H_Sword_Offhand", "2H_Sword", "Badge_Shield", "Rectangle_Shield", "Round_Shield", "Spike_Shield",
        "Knight_Helmet", "Knight_Cape",
        "1H_Wand", "2H_Staff", "Spellbook", "Spellbook_open", "Mage_Hat", "Mage_Cape",
        "1H_Crossbow", "2H_Crossbow", "Knife", "Knife_Offhand", "Throwable", "Rogue_Cape", "Icosphere"
    };

    // THE DRESSING OF THE GODS. Each hero is clothed from Assets/Models:
    // his body (FBX), his painted skin (Textures/<God>.png), his weapon
    // in hand and every other prop hidden, his clips wired to the flesh
    // driver. If the models are not in the project yet, this does
    // nothing and the capsule stands — the game never breaks for it.
    static void DressBody(GameObject heroGo, string heroName)
    {
        string body = null, tex = null, attack = "1H_Melee_Attack_Slice_Horizontal";
        string[] keep = null;
        switch (heroName)
        {
            case "THE MEDJAY": body = "Rogue"; tex = "Medjay"; attack = "1H_Ranged_Shoot"; keep = new[] { "2H_Crossbow", "Rogue_Cape" }; break;
            case "ARTEMIS": body = "Rogue"; tex = "Artemis"; attack = "1H_Ranged_Shoot"; keep = new[] { "2H_Crossbow", "Rogue_Cape" }; break;
            case "ANUBIS": body = "Rogue_Hooded"; tex = "Anubis"; keep = new[] { "Knife", "Knife_Offhand", "Rogue_Cape" }; break;
            case "HADES": body = "Rogue_Hooded"; tex = "Hades"; keep = new[] { "Knife", "Knife_Offhand", "Rogue_Cape" }; break;
            case "THOTH": body = "Mage"; tex = "Thoth"; attack = "Spellcast_Shoot"; keep = new[] { "2H_Staff", "Mage_Cape" }; break;
            case "ZEUS": body = "Mage"; tex = "Zeus"; attack = "Spellcast_Shoot"; keep = new[] { "2H_Staff", "Mage_Cape" }; break;
            case "ISIS": body = "Mage"; tex = "Isis"; attack = "Spellcast_Shoot"; keep = new[] { "1H_Wand", "Spellbook_open", "Mage_Cape" }; break;
            case "SEKHMET": body = "Barbarian"; tex = "Sekhmet"; keep = new[] { "1H_Axe", "1H_Axe_Offhand", "Barbarian_Cape" }; break;
            case "ARES": body = "Barbarian"; tex = "Ares"; keep = new[] { "2H_Axe", "Barbarian_Hat", "Barbarian_Cape" }; break;
            case "ATHENA": body = "Knight"; tex = "Athena"; keep = new[] { "1H_Sword", "Round_Shield", "Knight_Helmet", "Knight_Cape" }; break;
            default: return;
        }

        string fbxPath = "Assets/Models/" + body + ".fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (prefab == null) return;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, heroGo.transform);
        model.name = "Body";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        Bounds b = new Bounds(model.transform.position, Vector3.zero);
        bool any = false;
        foreach (var r in model.GetComponentsInChildren<Renderer>()) { b.Encapsulate(r.bounds); any = true; }
        if (any && b.size.y > 0.01f)
        {
            float f = 1.9f / b.size.y;
            model.transform.localScale = Vector3.one * f;
            model.transform.localPosition = new Vector3(0, -(b.min.y - heroGo.transform.position.y) * f, 0);
        }

        var capRend = heroGo.GetComponent<Renderer>();
        if (capRend != null) capRend.enabled = false;

        var keepSet = new System.Collections.Generic.HashSet<string>(keep);
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (System.Array.IndexOf(AllProps, t.name) >= 0 && !keepSet.Contains(t.name))
                t.gameObject.SetActive(false);

        string texPath = "Assets/Models/Textures/" + tex + ".png";
        // Crispness law: god skins import UNCOMPRESSED at full size with
        // anisotropic filtering — no soft, blurred paint on the gods.
        var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null && (imp.textureCompression != TextureImporterCompression.Uncompressed || imp.anisoLevel < 4))
        {
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.anisoLevel = 4;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }
        var texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (texAsset != null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Models/Materials"))
                AssetDatabase.CreateFolder("Assets/Models", "Materials");
            string matPath = "Assets/Models/Materials/" + tex + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.mainTexture = texAsset;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.material = mat;
        }

        var anim = model.GetComponent<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        anim.applyRootMotion = false;
        var driver = heroGo.AddComponent<BodyAnimator>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            var c = o as AnimationClip;
            if (c == null) continue;
            if (c.name.EndsWith("Idle")) driver.idleClip = c;
            else if (c.name.EndsWith("Walking_A")) driver.walkClip = c;
            else if (c.name.EndsWith("Running_A")) driver.runClip = c;
            else if (c.name.EndsWith("Death_A")) driver.deathClip = c;
            else if (c.name.EndsWith(attack)) driver.attackClip = c;
        }
    }

    static void BuildStructure(Transform root, string name, Team team, bool core,
        float x, float z, Material mat)
    {
        var go = Prim(PrimitiveType.Cylinder, name, root,
            new Vector3(x, core ? 3.25f : 4.6f, z),
            core ? new Vector3(3.2f, 6.5f, 3.2f) : new Vector3(3.4f, 9.2f, 3.4f), mat);
        var s = go.AddComponent<Structure>();
        s.team = team; s.isCore = core; s.radius = core ? 3.2f : 2.2f;
        // The stone wears its health where the eye can judge it.
        var tbar = go.AddComponent<OverheadBar>();
        tbar.height = core ? 10.5f : 12.3f;
        tbar.barScale = core ? 4.6f : 3.2f;
        if (!core)
        {
            // THE OBELISK — the maker's vision made stone: a tapered,
            // glyph-carved shaft on a stepped pad, a collar of its
            // court's color, and a burning blue crystal at the peak.
            // The root cylinder is the tower's invisible body — it
            // carries the collider and the laws; its renderer sleeps
            // and the obelisk shows in its place. (Children inherit
            // the root's 3.4 x 9.2 stretch — every part's world size
            // is divided back out by INV.)
            go.GetComponent<Renderer>().enabled = false;
            Vector3 inv = new Vector3(1f / 3.4f, 1f / 9.2f, 1f / 3.4f);
            var pad1 = Prim(PrimitiveType.Cube, "ObeliskPad", go.transform,
                new Vector3(x, 0.25f, z), Vector3.one, mat);
            pad1.transform.localScale = new Vector3(7.6f * inv.x, 0.5f * inv.y, 7.6f * inv.z);
            Object.DestroyImmediate(pad1.GetComponent<Collider>());
            var pad2 = Prim(PrimitiveType.Cube, "ObeliskPadTop", go.transform,
                new Vector3(x, 0.75f, z), Vector3.one, mat);
            pad2.transform.localScale = new Vector3(6.0f * inv.x, 0.5f * inv.y, 6.0f * inv.z);
            Object.DestroyImmediate(pad2.GetComponent<Collider>());
            var shaft = new GameObject("ObeliskShaft");
            shaft.transform.SetParent(go.transform);
            shaft.transform.position = new Vector3(x, 1.0f, z);
            shaft.transform.localScale = inv;
            shaft.AddComponent<MeshFilter>().mesh = ObeliskMesh(1.62f, 0.92f, 7.6f);
            shaft.AddComponent<MeshRenderer>().material = ObeliskMaterial(mat);
            var collar = Prim(PrimitiveType.Cube, "ObeliskCollar", go.transform,
                new Vector3(x, 8.85f, z), Vector3.one, mat);
            collar.transform.localScale = new Vector3(2.35f * inv.x, 0.5f * inv.y, 2.35f * inv.z);
            Object.DestroyImmediate(collar.GetComponent<Collider>());
            var plate = Prim(PrimitiveType.Cube, "ObeliskPlate", go.transform,
                new Vector3(x, 9.25f, z), Vector3.one, mat);
            plate.transform.localScale = new Vector3(2.6f * inv.x, 0.3f * inv.y, 2.6f * inv.z);
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            CrystalCluster(go.transform, new Vector3(x, 10.15f, z), 1f, inv);
            // The tower's shield, made visible: a pale bubble over the stone.
            var sm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sm.color = new Color(0.5f, 0.85f, 1f, 0.32f);
            sm.SetFloat("_Surface", 1f);
            sm.renderQueue = 3000;
            var bubble = Prim(PrimitiveType.Sphere, "TowerShield", go.transform,
                new Vector3(x, 5.4f, z), Vector3.one, sm);
            bubble.transform.localScale = new Vector3(8.0f * inv.x, 11.4f * inv.y, 8.0f * inv.z);
            Object.DestroyImmediate(bubble.GetComponent<Collider>());
        }
    }

    // The obelisk's body: a four-sided frustum rising to a pyramidion,
    // each face carrying the carved-glyph texture whole.
    static Mesh ObeliskMesh(float b, float t, float h)
    {
        var cs = new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
        var verts = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < 4; i++)
        {
            var c0 = cs[i]; var c1 = cs[(i + 1) % 4];
            int s0 = verts.Count;
            verts.Add(new Vector3(c0.x * b, 0, c0.y * b));
            verts.Add(new Vector3(c1.x * b, 0, c1.y * b));
            verts.Add(new Vector3(c1.x * t, h, c1.y * t));
            verts.Add(new Vector3(c0.x * t, h, c0.y * t));
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
            tris.AddRange(new[] { s0, s0 + 1, s0 + 2, s0, s0 + 2, s0 + 3 });
            int p0 = verts.Count;
            verts.Add(new Vector3(c0.x * t, h, c0.y * t));
            verts.Add(new Vector3(c1.x * t, h, c1.y * t));
            verts.Add(new Vector3(0, h + 0.85f, 0));
            uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0.5f, 1.12f));
            tris.AddRange(new[] { p0, p0 + 1, p0 + 2 });
        }
        var m = new Mesh();
        m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // A single crystal shard: an elongated octahedron.
    static Mesh CrystalMesh(float len, float rad)
    {
        float up = len * 0.62f, dn = len * 0.38f;
        var verts = new Vector3[]
        {
            new Vector3(0, up, 0), new Vector3(rad, 0, 0), new Vector3(0, 0, rad),
            new Vector3(-rad, 0, 0), new Vector3(0, 0, -rad), new Vector3(0, -dn, 0)
        };
        var tris = new int[]
        {
            0, 1, 2,  0, 2, 3,  0, 3, 4,  0, 4, 1,
            5, 2, 1,  5, 3, 2,  5, 4, 3,  5, 1, 4
        };
        var m = new Mesh();
        m.vertices = verts; m.triangles = tris;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Material CrystalMaterial()
    {
        string path = "Assets/Materials/ArenaCrystal.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = new Color(0.10f, 0.25f, 0.85f);
        m.SetColor("_EmissionColor", new Color(0.35f, 0.8f, 2.6f));
        m.EnableKeyword("_EMISSION");
        m.SetFloat("_Smoothness", 0.55f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material ObeliskMaterial(Material fallback)
    {
        string texPath = "Assets/Materials/ObeliskTex.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) return fallback;
        var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null && imp.wrapMode != TextureWrapMode.Repeat)
        {
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }
        string path = "Assets/Materials/ArenaObelisk.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_BaseMap", tex);
        m.color = Color.white;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CrystalShard(Transform parent, string name, Vector3 pos,
        float len, float rad, Material cm, Vector3 inv)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent);
        g.transform.position = pos;
        g.transform.localScale = inv;
        g.AddComponent<MeshFilter>().mesh = CrystalMesh(len, rad);
        g.AddComponent<MeshRenderer>().material = cm;
    }

    // The burning crown of the Vision: three blue shards, the heart
    // tall, two lesser flames at its feet.
    static void CrystalCluster(Transform parent, Vector3 pos, float scale, Vector3 inv)
    {
        var cm = CrystalMaterial();
        CrystalShard(parent, "CrystalMain", pos, 3.1f * scale, 0.62f * scale, cm, inv);
        CrystalShard(parent, "CrystalLeft",
            pos + new Vector3(-0.78f * scale, -0.55f * scale, 0.18f * scale),
            1.9f * scale, 0.42f * scale, cm, inv);
        CrystalShard(parent, "CrystalRight",
            pos + new Vector3(0.74f * scale, -0.62f * scale, -0.2f * scale),
            1.7f * scale, 0.38f * scale, cm, inv);
    }

    // THE MOVIE FILTER — the single step that makes flat shapes look
    // ALIVE: a global post-processing volume in the render pipeline's
    // own tongue (URP Volume, not the retired v2 stack): ACES filmic
    // tone, deeper color, and bloom so every emissive light in the
    // arena — trails, jewels, flashes, the river's gleam — bleeds
    // softly into the lens, the way his games glow.
    static void BuildPostFX()
    {
        var oldFx = GameObject.Find("PostFX");
        if (oldFx != null) Object.DestroyImmediate(oldFx);

        const string fxPath = "Assets/ArenaPostFX.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(fxPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, fxPath);
        }

        if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>(true);
        tone.mode.value = TonemappingMode.ACES;

        if (!profile.TryGet<ColorAdjustments>(out var grade)) grade = profile.Add<ColorAdjustments>(true);
        grade.saturation.value = 14f;
        grade.contrast.value = 10f;
        grade.colorFilter.value = new Color(1.03f, 1.0f, 0.95f);

        if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = 0.35f;
        bloom.threshold.value = 0.8f;
        bloom.scatter.value = 0.6f;

        if (!profile.TryGet<Vignette>(out var vig)) vig = profile.Add<Vignette>(true);
        vig.intensity.value = 0.24f;
        vig.smoothness.value = 0.5f;

        EditorUtility.SetDirty(profile);

        var fx = new GameObject("PostFX");
        var vol = fx.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.sharedProfile = profile;
    }

    // The sun dressed for war: warm late light, soft shadows with
    // strength enough to ANCHOR every body to the earth.
    static void DressSun()
    {
        foreach (var l in Object.FindObjectsByType<Light>())
        {
            if (l.type != LightType.Directional) continue;
            l.intensity = 1.2f;
            l.color = new Color(1f, 0.93f, 0.8f);
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.85f;
            l.shadowBias = 0.04f;
            l.shadowNormalBias = 0.4f;
        }
    }
}
#endif
