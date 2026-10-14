# FIVE-V-FIVE BLUEPRINT — MEDJAY: Arena of Kemet
**Knowledge before wisdom.** This document absorbs every reference Scroll Cipher provided and states the COMPLETE 5v5 match structure, so the roster round never has to be corrected later. Sources: 4 Tencent papers, the tencent-ailab/hokoff repo, and HoK wiki/guide research (links below).

---

## 1. The full HoK 5v5 structure (the reference)

### 1a. The five positions and where they stand
From Tencent's own macro-goals paper (NeurIPS 2021, §4.4), the standard 3-lane strategy:
- **Marksman, Mage, Warrior (or Tank)** take resources from the **three lanes**, respectively
- **Assassin** grabs the **neutral jungle** resources
- **Supporter roams** to assist them, *without gaining resources*

Lane assignment (verified across HoK guides):
- **Clash Lane (EXP lane)** — Fighters/Tanks; mage minions here give **+65 EXP each**
- **Farm Lane (gold lane)** — Marksmen; mage minions here give **+40 Gold each**
- **Mid Lane** — Mages; minions have **enhanced physical defense**; the mid laner is the roam connector between both jungles and side lanes
- **Jungle** — Assassins; high mobility, farm neutral camps, gank
- **Roam** — Supports/Tanks; decide when to gank or rotate, peel and shield

Map orientation: teams spawn at opposite **corners** (blue bottom-left, red top-right) on a diagonally symmetric map. The farm/clash lanes **flip by side** through the center — red team's gold lane is their top, clash their bottom. Our Gorge is built with exactly this symmetry (rotate 180°: TOP↔BOT).

### 1b. Hero type vocabulary (AAAI-20 paper)
Six hero types: **Mage, Assassin, Warrior, Marksman, Tank, Support** — with Tank/Support as the durable/defensive pair (unsuited to 1v1, core to 5v5). Draft paper confirms: heroes have **counter and supplement** relationships (a hero can be a natural counter to another, or amplify a teammate — e.g., MingShiyin amplifies Hou Yi's basic attack). Lineups are built as synergies, not piles of power.

### 1c. The macro-goal vocabulary (what pros actually decide)
MGG paper, Fig. 2–3: the map divides into **3 lanes, 4 jungle areas, 2 base areas**. Every strategic decision = two questions:
- **Region intent — "where to go"**: next region (a lane, a jungle area, a base)
- **Resource intent — "what to do"**: which resources to take (turrets, monsters, minions; gold; level)
Labels were extracted from pro matches with ~30s lookahead. **Our AI brains should think in exactly this vocabulary** — it's the pro brain's own frame.

### 1d. Jungle camps and buffs (all spawn at 00:30)
| Camp | Respawn | Gold | Effect |
|---|---|---|---|
| Azure Golem (Blue Buff) | 90s | 102 | kills restore 225 HP; **−20% cooldown, +3% mana/s**, 70s. Holder killed → buff transfers to killer |
| Crimson Golem (Red Buff) | 90s | 102 | kills restore 225 HP; basic attacks **slow 15% (stacks 2×) + Burn true damage**, 70s. Same transfer rule |
| Small camps (Firehawk, Fire Beak, Cheetah, Lizard, Spiketail, Tusked Boar) | 70s | 19–58 | economy; Firehawk flees, doesn't fight; Firehawk & River Sprites stop spawning at 04:00 |
| River Sprites | 60s | 63 | flee, don't fight; stop at 04:00 |

Standard jungle opening: start a buff at 00:30 → second buff ~00:45 → **first gank 01:15–01:30**. Buffs respawning 90s later create predictable invade windows (the timing math a jungler lives by).

### 1e. River objectives (the boss timeline)
| Objective | Spawns | Respawns | Effect |
|---|---|---|---|
| **Tyrant** (dragon) | 04:00 | 4 min | 71g + EXP to every team member; **Tyrant's Arrival**: chain-lightning attacks, cannot hit towers |
| **Overlord** | 04:00 | 4 min | 34g + EXP; **summon Overlord Vanguards**: next 2 minion waves on ALL 3 lanes become siege units with 50% tower-damage reduction |
| **Shadow Tyrant** | 10:00 | 3m30s | 136g + EXP; upgraded chain lightning + **+10% move speed (15% out of combat)** |
| **Shadow Overlord** | 10:00 | 3m30s | 58g + EXP; Vanguard push + **killer gains Summon Shadow Vanguard** (a super-siege down a chosen lane, leaves a Shadow Path: allies on it get 10% damage reduction + 10% structure damage) |
| **Tempest Dragon** | 20:00 (replaces Shadow pair) | 3 min | 170g + EXP; **Storm Awakening**: Tempest Vanguards that silence towers 5s; lightning strikes for **5% max-HP true damage**; **Blessing of Lightning**: shield 20→50% max HP, refreshes out of combat |

**Level-gating (the law our game already mirrors):** **Primal Bond** cuts damage taken from Tyrant/Overlord by 50% for 90s (60% for Shadow/Tempest). Bosses enrage (Primal Barrage-Rage) when the bond triggers. Our rule — *bosses sleep until a hero hits level 5* — is structurally this same law: objectives gated by level, not walked into cold.

### 1f. Minion waves
- Every wave: **1 melee + 2 ranged (mage)** minions
- **From 04:00**: one ranged minion per wave becomes a **ballista** (siege; reduced tower damage taken)
- **From 10:00**: ballistae become **cannons**
- **Super Minions**: destroying an enemy **high-ground tower** upgrades that lane's siege line (much higher attack + HP)
- Minion power ramps at **10:00 / 18:00 / 20:00** (speed then big stat boosts vs towers)
- Early game (0–4:00): **outer towers have shields** — increased defense vs heroes; don't dive, farm

### 1g. Structures
- **3 tiers per lane**: Outer Tower → Inner Tower → High Ground Tower, then the **Crystal** (the core)
- Crystal can only be attacked after **at least one** high-ground tower falls
- Crystal defenses: **regenerates 55 HP/s** when not attacked; **takes 55% less damage when no enemy minions are in range** (anti-backdoor); **consecutive hits on a hero ramp 60% each up to +300%**, ignoring defense
- High-ground tower falls → enemy super minions in that lane (backdoor threat)

### 1h. Draft (the pre-battle, JueWuDraft paper)
- 10-hero **lineups** picked from the full hero pool (102 in the paper)
- Pick order: **1-2-2-1-1-2-2-1-1-2** (alternating teams)
- Played as **best-of-N** series; a hero picked by your team **cannot be picked again** in later rounds
- Draft is a **counter/synergy game**: pick to strengthen your teammates' plan and counter the enemy's

### 1i. What tencent-ailab/hokoff actually contains (checked live)
- Folders `hok1v1/` and `hok3v3/` each contain only: `offline_eval/`, `offline_sample/`, `offline_train/`, `scripts/` — **AI training infrastructure, no game or map files**
- **No lane-coordinate files exist in the repo.** The actual game (map included) runs inside Tencent's separate proprietary **gamecore server** download; the repo is Apache-2.0 code for the training pipeline around it
- The paper's hero pools confirm structure expectations: the 1v1 arena pool is {Luban, Di Renjie, Hou Yi, Marco Polo, Gongsun Li} — **all marksmen**

---

## 2. GAP TABLE — our build vs full 5v5 structure

| Dimension | Full 5v5 (reference) | Our build today | Gap to close |
|---|---|---|---|
| Heroes per side | 5 | 1 (Medjay vs Ares) | +4 seats/side, AI-driven; roster of 10 heroes |
| Roles | Marksman/mage/warrior-or-tank in 3 lanes; assassin jungles; supporter roams, takes nothing | 1 all-rounder hero per side | role assignment + lane economy split |
| Lane economy | Farm-lane mages +40 gold; clash-lane mages +65 EXP; mid minions enhanced physical defense | flat gold/XP per minion | gold/EXP lane mechanics |
| Jungle camps | 2 buff golems (90s) + 6 small camps (70s) + river sprites (60s, stop 04:00); all spawn 00:30 | 4 scarab packs (550 HP, 50s), 2 crocs (1500 HP, 90s, Sobek buff 60s) | camp variety + start/respawn timing grid; buff-transfer rule |
| Objectives | Tyrant+Overlord 04:00 (4min), Shadow pair 10:00 (3m30s), Tempest 20:00 (3min); level-gated (Primal Bond) | Apophis + Sphinx sleep until first hero lvl 5, 150s respawn, kill = summon token | escalation timeline (Shadow/Tempest analogs) as late game; our gate = level law already ✓ |
| Minion waves | 1 melee + 2 ranged; ballista 04:00; cannon 10:00; super minions on high-ground kill; ramps 10/18/20 | 3 melee + 1 ranged every 22s; no siege escalation | ballista/cannon/super-minion line |
| Towers | 3 tiers/lane (outer/inner/high-ground); outer shields 0–4:00 | 2 towers/lane (outer/inner) + core | high-ground tier + shield + crystal regen/anti-backdoor/hit-ramp |
| Draft | 10-hero lineups, 1-2-2-1-1-2-2-1-1-2 order, bans, best-of-N | none (fixed 1v1) | draft phase when the roster exists |
| HUD | full match HUD | HUD.cs waits for a Canvas that doesn't exist | builder-raised HUD (planned) |
| Vision | brushes, vision sprites, warding | no vision mechanics | later phase |

---

## 3. Proposed 5-per-side role layout on OUR Gorge
*(God names left BLANK — his picks.)*

Our Gorge is diagonally symmetric like HoK's map, so roles mirror through the center — exactly as blue/red swap farm and clash lanes.

**KEMET (pyramid, SW home)**
| Seat | Road | Role | HoK lane equivalent | God |
|---|---|---|---|---|
| 1 | BOT road (SE rim) | **Marksman** (ranged carry, farms the gold lane) | Farm Lane | _______ |
| 2 | MID road (diagonal) | **Mage** (burst, roams between lanes) | Mid Lane | _______ |
| 3 | TOP road (NW rim) | **Warrior or Tank** (brawler, survives the EXP lane) | Clash Lane | _______ |
| 4 | Jungle | **Assassin** (jungler: clears camps, ganks from 01:15) | Jungle | _______ |
| 5 | Roam | **Support** (peels, shields, rotates — takes no farm) | Roam | _______ |

**OLYMPUS (temple, NE home) — mirrored through the center**
| Seat | Road | Role | HoK lane equivalent | God |
|---|---|---|---|---|
| 1 | Their TOP road (their gold lane) | **Marksman** | Farm Lane | _______ |
| 2 | MID road | **Mage** | Mid Lane | _______ |
| 3 | Their BOT road (their EXP lane) | **Warrior or Tank** | Clash Lane | _______ |
| 4 | Jungle | **Assassin** | Jungle | _______ |
| 5 | Roam | **Support** | Roam | _______ |

Fitting the current heroes: the Medjay (bow, ranged) reads naturally as the **Kemet Marksman** on the BOT road; Ares (spear/shield bulk) as the **Olympus Warrior** on their clash road. The player's seat = the Medjay's; the other nine run AI brains.

Boss mapping (his law preserved): **Apophis** (chaos serpent) = the **Overlord** role — kill him, summon the push (our summon-on-kill IS the vanguard effect). **The Sphinx** = the **Tyrant** role — the team-fight buff objective. Tempest Dragon = a future third, late-game escalation, when he wants it.

---

## 4. Build order: 1v1 → working 5v5 (7 steps)
1. **Lock the roster law**: he names the 5 gods per side; we write the role table (§3) into stone. Nothing else starts until the names are his.
2. **Hero definitions as data**: a `HeroDef` per god (base stats, the 4 abilities, hero type) — 10 definitions; Medjay's stays the master copy it already is. Abilities derive from his Q/W/E/R kit pattern.
3. **The AI driver**: generalize HeroAI into an AI brain for all 9 non-player seats — thinking in the macro-goal vocabulary (§1c: region intent = assigned road/jungle/region; resource intent = farm/push/objective), plus the four temperaments (push-fast / farmer / hunter / split) choosing *how* it plays the role.
4. **Jungle grid rebuild**: two buff golems (Azure/Crimson analogs — one of ours is already Sobek's croc; keep it, rename or keep), small camps with variety, **00:30 start / 90s buff / 70s small** respawn grid, buff-transfer-on-kill. Bosses keep his level-5 gate (the Primal Bond law is already his).
5. **Objective escalation**: Shadow-tier and a Tempest-tier late boss (or boss evolution) — proposed, then built on his word; summon-on-kill stays the Overlord mechanic.
6. **Wave + structure rules**: ballista (04:00) / cannon (10:00) / super-minion (high-ground kill) line; decide the high-ground tower question (add 3rd tier or hold at 2); crystal anti-backdoor + 55 HP/s regen + outer shields 0–4:00.
7. **HUD + 5v5 shakedown**: builder-raised HUD (health/mana, cooldown sweeps, score, timer, announcements for all 10 seats); full 5v5 playtest loop; boss summons and lane AI verified.

---

## 5. Sources
- Papers (local): `~/workspace/user/files/Mastering_Complex_Control_in_MOBA_Games_with_Deep_Reinforcement_Learning.pdf`, `~/workspace/user/files/NeurIPS-2023-hokoff-real-game-dataset-from-honor-of-kings-and-its-offline-reinforcement-learning-benchmarks-Paper-Datasets_and_Benchmarks.pdf`, `~/workspace/user/files/2012.10171v4.pdf` (JueWuDraft), `~/workspace/user/files/NeurIPS-2021-learning-diverse-policies-in-moba-games-via-macro-goals-Paper.pdf`
- https://github.com/tencent-ailab/hokoff (code only; game runs in Tencent's separate gamecore — no map/lane files in the repo)
- https://esports.gg/news/honor-of-kings/hok-jungle-monster/ (camp stats, buffs, objective spawn/respawn times, Primal Bond)
- https://news.bittopup.com/news/honor-of-kings-jungle-invade-guide-steal-blue-buffs-s18 (jungle opening, gank timings, invade windows)
- https://honor-of-kings.fandom.com/wiki/Phases (match phases, turret shields, objective priority, super minions)
- https://honor-of-kings.fandom.com/wiki/Mage_Minion (gold/EXP lane assignment, wave composition, stat ramps)
- https://honor-of-kings.fandom.com/wiki/Ballista_Minion · /Super_Minion · /Melee_Minion (siege escalation)
- https://honor-of-kings.fandom.com/wiki/Crystal (attack rules, regen, anti-backdoor, hit-ramp)
- https://honor-of-kings.fandom.com/wiki/Map_elements (structure list)
- https://gaminggblog.com/honor-of-kings-map/ (corner spawns, diagonal river, rotation rules)
- https://www.sportskeeda.com/esports/best-honor-kings-beginners-guide (roles per lane)

*Deliberately not claimed (no source verified them): exact seconds between minion waves, Primal Bond's numeric level threshold, per-quadrant camp placement.*
