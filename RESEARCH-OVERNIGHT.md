# OVERNIGHT RESEARCH — MOBA Building for MEDJAY: Arena of Kemet
**Written:** October 14, 2026 (while you slept)
**Project state it assumes:** Unity 6.6 (6000.6.5f1), Universal 3D template, scripts on the new Input System, editor builder raising the map, single-player vs AI. Mobile next, online last.

---

## 1) THE BODIES PIPELINE — Mixamo into Unity 6, the exact road

This is the next build round: real people walking. The whole pipeline below is verified against current docs and community guides (links at the end).

### A. On Mixamo (mixamo.com)
1. **Free Adobe account** — sign in (required; he enters his own login).
2. **Characters tab** — pick a body. For our two heroes, search for: an archer-style figure (Medjay) and a heavy warrior (Ares). Note: Mixamo's characters are generic modern/fantasy — no Egyptian/Greek skins. We recolor/kitbash later (see section 2).
3. **Animations tab** — select the same character, and download clips by category. Search terms to use:
   - `idle` → "Standing Idle"
   - `walk` / `run` → "Walking", "Running"
   - `death` → "Dying" / "Death"
   - bow → search `arrow` ("Standing Draw Arrow", "Bow Shot"-style clips)
   - sword → search `slash` / `attack` ("Standing Melee Attack"-style clips)
   - also grab a `hit reaction` clip for later.
4. **Download settings (important):**
   - Format: **FBX for Unity (.fbx)**
   - Skin: **With Skin** for the base character; **Without Skin** for extra animation-only clips (just the skeleton motion, applied to our rig)
   - Keyframe Reduction: **None** (best quality)
   - FPS: **30**
   - **In Place: CHECKED for walk/run locomotion.** Our code moves the transform; the animation must not move it too, or feet slide double.

### B. In Unity (import)
1. Drop the FBX files into `Assets/` (suggest `Assets/Characters/`).
2. Select the FBX → Inspector → **Rig tab**:
   - Animation Type: **Humanoid**
   - Avatar Definition: **Create From This Model**
   - Click **Apply**
3. Click **Configure** → check the bone map: no red errors. All green = the rig retargets correctly.
   - If the model shows a frozen T-pose in play: **Rig tab → Enforce T-Pose → Done → Apply.**
4. **Animation tab** (per clip): tick **Loop Time** (idle/walk/run). For in-place locomotion: Root Transform Rotation → **Bake Into Pose**, Root Transform Position (Y) → **Bake Into Pose**, Position (XZ) → **Bake Into Pose**. Click Apply.

### C. The Animator Controller
1. Right-click Project → **Create → Animator Controller** (e.g., `MedjayController`).
2. Add an **Animator** component to the model GameObject; assign the controller.
3. States: **Idle** (default), **Walk**, **Run**, **Attack**, **Death**.
4. Parameters: float **Speed**; triggers **Attack**, **Die**.
5. Transitions:
   - Idle → Walk/Run: `Speed > 0.1` (blend Walk→Run around `Speed > 3.5`)
   - Walk/Run → Idle: `Speed < 0.1`
   - Any State → Attack: trigger **Attack** (exit time off, short transition)
   - Attack → Idle: on exit
   - Any State → Death: trigger **Die** (no transition time)
6. **Wiring to code:** our Unit/Hero already knows velocity and combat. In the build round we add, roughly: `animator.SetFloat("Speed", currentMoveSpeed)` each frame, `SetTrigger("Attack")` on fire, `SetTrigger("Die")` on death. The brain doesn't change — animation listens to it.
7. **The swap:** make the model a child of the hero GameObject, hide/disable the capsule mesh renderer. Keep colliders and scripts where they are.

### D. Failure points and fixes (the ones that bite beginners)
| Failure | Fix |
|---|---|
| Character frozen in T-pose, no animation | Rig tab → Animation Type **Humanoid** (not Generic), Apply; Configure → no red errors |
| Bones mapped wrong (red X in Configure) | Configure → map manually: Hips, Spine, Head, L/R UpperArm→Hand chain, L/R Thigh→Foot chain |
| Feet slide / character drifts | Animation was downloaded NOT in place while code also moves → re-download with **In Place** checked, Bake root transform into pose |
| Model giant or tiny vs arena | FBX import scale should be 1; our heroes are ~2 units tall — rescale the GameObject so the model matches |
| Arms/legs twist weirdly on retarget | Muscle & Settings in Configure Avatar; adjust bone limits; or try a different source clip |
| Attack animation too fast/slow vs real hits | Tune the state's **Speed** multiplier or re-time the clip; the damage still lands by code timing |

### E. Mixamo status & alternatives
- **Mixamo is alive and free** as of 2026: humanoid characters only, thousands of clips, no API. Free for personal and commercial use; you may **not resell or redistribute the raw assets** — using them inside a built game is the standard allowed use (re-check the FAQ at time of download).
- If Mixamo is down or a body won't auto-rig: **AccuRIG** (browser auto-rig, incl. non-humanoid), **ActionForge** (free browser auto-rig + CC0 animation library from Quaternius), or **Blender Rigify** (full control, steepest learning curve).

---

## 2) FREE CHARACTER SOURCES — rigged, usable, for our war

Rule used: only sources verified by actual pages/search results. Nothing Egyptian/Greek-specific that is rigged *and* free turned up — plan around it (bodies from below + color/props kitbash).

- **Mixamo** (mixamo.com) — free, Adobe account. Hundreds of characters + thousands of animations (walk/run/attack/death, incl. bow and sword sets). Best for: the hero bodies AND all animation clips. Caveat: generic designs; recolor/prop them. License: free for personal + commercial use; no resale/redistribution of raw files.
- **Quaternius** — https://quaternius.com — **CC0 (public domain, commercial OK, no attribution needed).** Low-poly stylized, adult-proportioned, **rigged and animated** humanoids: the RPG Character Pack (warrior/wizard/cleric/monk/ranger/rogue) and the Universal Animation Library (250+ clips). Formats: FBX/OBJ/GLB. Good bodies with an Egyptian recolor (gold/brown skin tones via their textures).
- **KayKit Adventurers** — https://kaylousberg.itch.io/kaykit-adventurers — **CC0.** 5 fully **rigged and animated** characters + **25+ weapons incl. swords, shields, axes, BOW/crossbow, arrows, staff**. Low-poly, mobile-optimized, FBX/GLTF. The bow pack is our Medjay's quiver.
- **KayKit Skeletons** — https://kaylousberg.itch.io/kaykit-skeletons — **CC0.** 4 rigged/animated skeletons + weapons. Future enemy variety.
- **Kenney** — https://kenney.nl — **CC0 site-wide.** Props, weapons, environment kits (characters mostly unanimated). Good for set dressing, Khopesh-style blades.
- **Poly Pizza** — https://poly.pizza — free API-less search across Kenney + Quaternius + community models; good for hunting a specific prop (spear, shield, bow).
- **OpenGameArt** — mixed licenses per file; fallback only, audit each download.

**Recommended picks for our two:**
- **The Medjay** = KayKit Adventurer with bow (CC0, zero license risk) **or** a Mixamo archer body. Gold/dark recolor for Kemet identity.
- **Ares** = Quaternius RPG warrior (CC0) **or** a Mixamo heavy warrior, with spear + shield as child props on the hand bones.
- **Weapons as props:** parent a bow/spear/shield mesh to the hand bone — no re-rig needed.
- **Silhouette law** (from the Jokela thesis): Medjay reads as *bow + gold*, Ares as *spear/shield bulk + bronze-red*. Readable in one glance from the high camera.

---

## 3) UNITY 6 / URP GOTCHAS — traps to dodge

**Already bitten, already fixed (for the record):**
- **(a)** New Input System active → legacy `UnityEngine.Input` throws. Fixed: scripts rewritten to `UnityEngine.InputSystem`.
- **(b)** Editor builder scripts don't run `Awake`. Fixed: beast health wired explicitly in the builder.

**Android build prep (do now, build later):**
1. Unity Hub → Installs → your Unity 6.6 → Manage → **Add modules**: Android Build Support, OpenJDK, Android SDK & NDK Tools.
2. File → **Build Profiles** → Android → **Switch Platform** (reimport takes a while — good overnight task).
3. Project Settings → **Player (Android tab):** set a Package Name (e.g. `com.medjayscrolls.arenaofkemet` — placeholder), Minimum API Level 26+ (Android 8, wide coverage), Scripting Backend **IL2CPP**, Target Architectures **ARM64** only (drops ARMv7, smaller file).
4. Graphics for mobile: **disable HDR**, MSAA 2x or off, **one** directional light, no realtime shadows on phones. URP Asset: Additional Lights **off**, Soft Shadows off.
5. **Do touch controls last-minute, not now:** laptop build stays mouse+keyboard; mobile gets a virtual joystick + tap-to-move later. Same game, different input skin.
6. **Sign the APK later** (keystore) — only when shipping a real file to a phone.

**Performance (URP, low-end laptop → phones):**
- Keep the **SRP Batcher on** (default in URP) — it cuts CPU cost per draw call. All our shared gold/stone materials already batch well.
- Targets: **<100–150 draw calls on phones**, <1000 on desktop. One camera only (each camera = a full render pass).
- Never use the **Complex Lit** shader on mobile (we use URP Lit — fine). No fullscreen post-processing on phones (no bloom/AO/DOF).
- Avoid `MaterialPropertyBlock` on many objects (breaks SRP batching) and keep shader keywords minimal.
- Our primitive-built map is fine for now; when props multiply, prefer **shared materials** (already our pattern via `GetMat`).

**The HUD (our unbuilt Canvas):** `HUD.cs` looks up named objects (HpBar, MpBar, XpBar, CdQ/W/E/R, announcements…) that don't exist yet — that's why it silently no-ops.
- Cleanest beginner path: **one Canvas, Render Mode = Screen Space – Overlay**, with a **CanvasScaler → Scale With Screen Size** (reference 1280×720, landscape).
- Raise it by code: an **"Arena → Build HUD"** step in the builder (same one-click pattern as the map — he already knows this motion). Layout:
  - bottom-center: ability bar — Q W E R buttons, cooldown via **Image fillAmount** (radial)
  - bottom-left: portrait + HP / MP / XP bars (filled Images or Sliders)
  - top-center: kill score + match timer
  - center: announcements (fade in/out Text)
  - keys B (recall) / F (heal) / T (summon) as small labeled slots with cooldown overlays
- **Minimap later:** a second top-down camera → **RenderTexture** → **RawImage** in a corner. Cheap and standard.
- Health bars over units: small **World Space** canvases pooled on units, camera-facing (billboard). Keep them pooled, not created per frame.

---

## 4) ARES' BRAIN — what the Tencent papers give a rule-based AI

Read all four papers in `~/workspace/user/files/`. No RL training is possible (no compute) — these are the parts that translate directly into **rules**:

**The MGG paper (NeurIPS 2021, Macro-Goals)** — the most useful one. It models a MOBA mind as two intents:
- **Region Intent — "where to go":** which map region matters now (each lane, jungle areas, base).
- **Resource Intent — "what to do":** turrets, minions, jungle monsters, gold.
- Our HeroAI already does a rough version (lane scoring + beast hunting). The upgrade: compute *both intents* each think-tick and weight them.

**The four temperaments as intent weightings (implementable):**
| Temperament | Region (where) | Resource (what) | Switch condition |
|---|---|---|---|
| **Push-fast** | deepest enemy-side lane region | turrets first, ignores jungle | when ahead (more towers/gold) |
| **Farmer** | safest region, far from enemies | jungle monsters + own minions' gold | when behind; avoids fights until a level/gold threshold |
| **Hunter** | the player's current region | the player hero over everything | when the player is fed (high kills/level) |
| **Split** | lane farthest from the player | minions then turrets | when even; forces the player to run |

- **Switch rule:** re-evaluate temperament every ~60 seconds or on big events (tower lost, boss slain, player death). This is the paper's "diversity" insight in rule form: one enemy, four tempers — unsolvable in two matches.

**Mastering Complex Control (AAAI 2020):**
- **Macro/micro split:** we already have it (ChooseLane = macro, combat = micro). Keep the layers separate — strategies change the macro layer; fighting skill stays in micro.
- **Target selection:** score candidates on **(low HP × distance × threat)**, not just nearest. Rule: focus the lowest-HP enemy hero in range first; if none, the highest-damage minion.
- **Retreat heuristic:** when own HP% drops below the estimated enemy damage in range, back toward the nearest living tower. (Our dive-guard is the mirror of this for the player.)

**JueWuDraft (arXiv:2012.10171):** hero drafting for future roster design. Rules for every new god we add:
- Give each hero a **role** (marksman / mage / tank / support / assassin-flavored).
- Every hero needs at least **one hard counter and one synergy partner**. Example: Ares' spear beats shield-walls; Medjay's bow outranges melee but loses to fast divers.
- The draft pick order (1-2-2-1…) only matters once the roster has 4+ heroes.

**HOKoff (NeurIPS 2023):** an offline-RL *dataset* — nothing implementable without RL compute (stated honestly). Its useful residue:
- The **watch-list** pros track: HP, mana, gold, level, cooldowns, turret states, positions. That's exactly the dashboard Ares' rules should read — we're already close.
- Its **subtask framing** (destroy turret / gain gold as separate objectives) matches the intent model above.

---

## Sources cited
- Mixamo import workflow: https://github.com/jiyuu-jin/rivals/blob/HEAD/app/Assets/Scripts/MixamoAnimationSetup.md
- Unity FBX/Rig import steps: https://github.com/eazytom/asset-assembly-automator/blob/HEAD/midjourney_meshy_unity_mcp_character_workflow.md
- Mixamo rig pitfalls: https://dev.to/orca_forge/the-cause-of-wrist-skin-collapse-is-skinning-not-pose-583k
- Mixamo alternatives: https://magiclight.ai/academy/mixamo-alternative/
- Browser auto-rig alternative: https://www.youtube.com/watch?v=p779LRtmuYo
- Quaternius (CC0): https://quaternius.com
- KayKit Adventurers (CC0): https://kaylousberg.itch.io/kaykit-adventurers
- KayKit Skeletons (CC0): https://kaylousberg.itch.io/kaykit-skeletons
- Kenney (CC0): https://kenney.nl
- Poly Pizza: https://poly.pizza
- URP performance tuning: https://github.com/wobok/dots/blob/HEAD/Packages/com.unity.render-pipelines.universal/Documentation~/configure-for-better-performance.md
- Unity perf checklist: https://github.com/ryanjbrant/xrai/blob/HEAD/KnowledgeBase/_UNITY_PERF_OPTIMIZATION_INSIGHTS.md
- Canvas/UI guidance: https://github.com/cansinoleighann-afk/grilsfighting/blob/HEAD/.codex/skills/funplay-unity-ui-composition/SKILL.md
- Unity HUD tutorial: https://DEV.to/genomagames/genoma-invaders-using-unity-ui-for-in-game-huds-pjc
- Minimap approach: https://medium.com/@alessandrovalcepina/creating-a-rts-like-minimap-with-unity-9cd578dc4522
- Papers (local): `~/workspace/user/files/Mastering_Complex_Control_in_MOBA_Games_with_Deep_Reinforcement_Learning.pdf`, `~/workspace/user/files/NeurIPS-2023-…Hokoff…pdf`, `~/workspace/user/files/2012.10171v4.pdf` (JueWuDraft), `~/workspace/user/files/NeurIPS-2021-learning-diverse-policies-in-moba-games-via-macro-goals-Paper.pdf`
- Thesis (local): `~/workspace/user/files/fae_jokela.pdf` (Character Design and Customization in MOBA Games — silhouette/color identity law)

---

## Recommended first moves when we resume
1. **Confirm the controls:** drop the fixed Hero.cs (12031), Play, right-click roams the full map, Q dashes — Console quiet.
2. **Pick the two bodies:** KayKit Adventurer with bow (CC0, zero license risk) for the Medjay; Quaternius RPG warrior (CC0) + spear/shield props for Ares.
3. **One Mixamo session:** free Adobe account; download idle / walk / run / death + bow-draw/shoot + sword-slash clips, FBX for Unity, **In Place** checked for locomotion.
4. **Bodies round build:** import as Humanoid, one Animator Controller (Speed float + Attack/Die triggers) wired to our movement code, hide the capsules — brains unchanged.
5. **Raise the HUD** with an "Arena → Build HUD" builder step: HP/MP/XP bars, QWER cooldown icons, score + timer, announcements.
