import bpy, os

SRC = os.path.expanduser("~/workspace/your_files/arena-of-kemet-unity/bodies/kaykit/KayKit-Game-Assets-KayKit-Character-Pack-Adventures-1.0-672074b/addons/kaykit_character_pack_adventures/Characters/gltf")
OUT = os.path.expanduser("~/workspace/your_files/arena-of-kemet-unity/bodies/fbx")
os.makedirs(OUT, exist_ok=True)

KEEP = {
    "Idle", "Walking_A", "Running_A", "Death_A", "Hit_A", "Block",
    "1H_Melee_Attack_Slice_Horizontal", "2H_Melee_Attack_Chop",
    "Spellcast_Shoot", "Spellcasting", "1H_Ranged_Shoot", "1H_Ranged_Shooting",
}

for name in ["Barbarian", "Knight", "Mage", "Rogue", "Rogue_Hooded"]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(SRC, name + ".glb"))
    for a in list(bpy.data.actions):
        if a.name not in KEEP:
            bpy.data.actions.remove(a)
    print(name, "kept actions:", [a.name for a in bpy.data.actions])
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT, name + ".fbx"),
        use_selection=False,
        object_types={'ARMATURE', 'MESH'},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_simplify_factor=0.0,
        apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z', axis_up='Y')
    print("EXPORTED", name)
print("ALL DONE")
