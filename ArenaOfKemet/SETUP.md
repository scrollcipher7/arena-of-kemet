# MEDJAY: ARENA OF KEMET — Unity Setup Guide
*Your first 3D game, built the way Honor of Kings was built: in Unity.*
*One action per step. Do them in order. Any red error — screenshot it and send it to me, I fix the script.*

---

## PART 1 — Install Unity (tonight; the download is the long part)

1. Go to **unity.com** and click **Download Unity Hub**. It's free.
2. Install Unity Hub and open it.
3. Sign in (create a free Unity account if it asks).
4. In Unity Hub, click the **gear icon** (top right) → **Licenses** → **Add license** → **Get a free personal license**. Done — Unity Personal is free.
5. In Unity Hub, click **Installs** (left side) → **Install Editor**.
6. Pick **Unity 2022.3 LTS** (if a newer LTS is listed, that one is fine too).
7. On the next screen, check these two boxes: **Android Build Support** and **Windows Build Support**. Then click **Install**.
8. Let it download — it's big (10 GB+). This is the part you let run while you do other things.

## PART 2 — Create the project

1. In Unity Hub, click **Projects** → **New project**.
2. Choose the template **Universal 3D** (the Core list — NOT High Definition; HDRP is built for heavy gaming PCs and chokes phones and modest laptops. Universal is the pipeline mobile MOBAs use).
3. Name it **ArenaOfKemet**. Leave the location as-is.
4. Click **Create project**. The editor opens — first open is slow, that's normal.
5. Download the file I sent you: **arena-of-kemet-unity-kit.zip**. Unzip it.
6. Inside your new project folder there is an **Assets** folder. Inside Assets, make a new folder called **Scripts**.
7. Copy all the **.cs files** from the kit's `ArenaOfKemet/Assets/Scripts` folder into your project's `Assets/Scripts` folder.
8. Click back into the Unity editor and wait — it will compile the scripts (a small progress bar). If the Console at the bottom shows no red errors, the code is alive.

## PART 3 — Build the map: THE ONE-CLICK BUILDER (use this)

The map is the full Honor of Kings layout — THREE lanes and the jungle — and you do NOT place it by hand:

1. In Unity's Project panel, right-click **Assets** → **Create → Folder** → name it **Editor**.
2. From the kit, drag **ArenaSceneBuilder.cs** (it's in `ArenaOfKemet/Assets/Editor/`) into that Editor folder.
3. Also re-copy the kit's **Scripts** files over your old ones (the 3-lane update: waves now march all three lanes).
4. In the top menu click **Arena → Build 3-Lane Map**. A popup confirms: 3 lanes, river, jungle, 12 towers, 2 temples, both heroes, camera set.
5. **Ctrl+S** to save the scene. Then skip to PART 4 and press Play.

The steps below are the old by-hand path — kept as backup only.

## PART 3B — Build the lane by hand (backup path)

**Camera**
1. In the Hierarchy (left panel), click **Main Camera**.
2. In the Inspector (right panel), set **Position** to X **0**, Y **38**, Z **-26** and **Rotation** to X **55**, Y **0**, Z **0**.

**Ground materials** (do this once)
1. In the Assets panel (bottom), right-click → **Create → Material**. Name it **SandDark**. Click it, click the **Albedo** color box, pick a dark sand brown.
2. Make three more materials the same way: **StoneLane** (light grey-tan), **RiverBlue** (deep blue), **GoldKemet** (gold), **StoneOlympus** (pale blue-grey).

**The field**
1. Menu **GameObject → 3D Object → Plane**. Name it **Ground**. Scale: X **8**, Y **1**, Z **3**. Drag **SandDark** onto it.
2. Another Plane, name **Lane**, Position Y **0.02**, Scale X **7.6**, Y **1**, Z **0.9**. Drag **StoneLane** onto it.
3. Another Plane, name **River**, Position Y **0.03**, Scale X **0.5**, Y **1**, Z **3**. Drag **RiverBlue** onto it.

**The structures** — for each one: **GameObject → 3D Object → Cylinder**, rename it, set Position and Scale, then in the Inspector click **Add Component** and add **Structure**, set its **Team**, and tick **Is Core** only on the two cores. Set **Radius** to **3.2** on the cores.

| Name | Position X | Y | Scale | Team | Core? |
|---|---|---|---|---|---|
| TempleCore | -30 | 3 | 3, 6, 3 | Kemet | YES |
| KemetTower2 | -19 | 2.25 | 2, 4.5, 2 | Kemet | no |
| KemetTower1 | -8 | 2.25 | 2, 4.5, 2 | Kemet | no |
| OlympusTower1 | 8 | 2.25 | 2, 4.5, 2 | Olympus | no |
| OlympusTower2 | 19 | 2.25 | 2, 4.5, 2 | Olympus | no |
| ShrineCore | 30 | 3 | 3, 6, 3 | Olympus | YES |

Drag **GoldKemet** onto the three Kemet structures and **StoneOlympus** onto the three Olympus ones. (Z position is 0 for all of them.)

**The heroes**
1. **GameObject → 3D Object → Capsule**. Name it **Medjay**. Position X **-26**, Y **1**, Z **0**. Drag **GoldKemet** onto it.
2. **Add Component → Hero**. Set: **Is Player** = ticked ✓, **Hero Name** = THE MEDJAY, **Team** = Kemet, **Attack Range** = **6.5** (he fights with the bow).
3. Another Capsule. Name it **Ares**. Position X **26**, Y **1**, Z **0**. Drag **StoneOlympus** onto it.
4. **Add Component → Hero**. **Is Player** = unticked, **Hero Name** = ARES, **Team** = Olympus, **Attack Range** = **3.4**.
5. With Ares still selected, **Add Component → HeroAI**. That's his brain.

**The director**
1. **GameObject → Create Empty**. Name it **GameManager**.
2. **Add Component → Game Manager** and **Add Component → HUD**. (The HUD waits for Part 5 — the game runs without it.)

## PART 4 — Press Play

1. Press **Ctrl+S** and save the scene as **Arena**.
2. Click the **Play** button (▶ at the top center).
3. Minion waves march from both temples and clash at the river. **Click the ground** to move the Medjay — he auto-fires his bow in range. **Q** dash, **W** falcon, **E** shield, **R** the flood (unlocks at level 6).
4. Click **Play** again to stop.

Something breaks or shows red? Screenshot the Console and send it to me — the fix comes back as a new script file, you drop it over the old one.

## PART 5 — Next round (we do this together after your first Play)

- The full HUD: health/mana bars, cooldown gems, shop buttons, kill announcements. The script already knows the names it looks for: HpBar, MpBar, XpBar, LevelText, GoldText, KdText, ScoreText, ClockText, AnnounceText, AnnounceSub, CdQ, CdW, CdE, CdR, BladeBtn, ArmorBtn, BootsBtn, AbQ, AbW, AbE, AbR, DeathVeil, EndPanel.
- Then real 3D models replace the greybox shapes — heroes, towers, temples from the Unity Asset Store (free packs first).
- Then the Android build: **File → Build Settings → Android → Build** — that's the moment the game becomes an APK for your phone, like your apps.

*You direct. I write. The lane rises.*
