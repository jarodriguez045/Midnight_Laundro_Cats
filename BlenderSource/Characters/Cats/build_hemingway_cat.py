"""
build_hemingway_cat.py
────────────────────────────────────────────────────────────────────────────────
Builds a black-and-white calico Hemingway (polydactyl) cat from scratch using
Blender's Python API, then exports a Unity-ready FBX.

Design:
  • Rounded clay shapes — body parts built from UV-spheres, cones, and cylinders
    with smooth shading for a soft toy / clay aesthetic.
  • Calico colouring — white body, black saddle/tail, orange ears and face patch,
    pink nose.  Each region is a separate mesh piece assigned to its own material.
  • Hemingway paws — front paws are significantly oversized and each has an extra
    polydactyl toe cluster.
  • Simple rig — 20-bone armature (root → spine → head, tail, four limbs).
  • Every mesh vertex belongs to exactly one vertex group, named after the bone
    that should control it.  Armature modifier is set up on the joined mesh.

Usage:
  Blender (UI):
    Open Scripting workspace, paste or load this file, press Run Script.
  Blender (headless):
    blender --background --python build_hemingway_cat.py

Output:
    ../../Exports/FBX/HemingwayCat.fbx
    (relative to this script's directory)

Compatibility: Blender 3.6 LTS and Blender 4.x.
Unity import:  Use default FBX import settings.  A 90° X-axis rotation on the
               model root is expected and normal for Blender → Unity exports.
────────────────────────────────────────────────────────────────────────────────
"""

import bpy
import math
import os

# ──────────────────────────────────────────────────────────────────────────────
# CONFIG
# ──────────────────────────────────────────────────────────────────────────────

_SCRIPT_DIR = os.path.dirname(os.path.realpath(__file__))
EXPORT_PATH = os.path.normpath(
    os.path.join(_SCRIPT_DIR, "..", "..", "Exports", "FBX", "HemingwayCat.fbx")
)
CAT_NAME = "HemingwayCat"

# ──────────────────────────────────────────────────────────────────────────────
# SCENE UTILITIES
# ──────────────────────────────────────────────────────────────────────────────

def clear_scene():
    """Delete every object, mesh, material, and armature in the current scene."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for collection in (
        bpy.data.meshes,
        bpy.data.materials,
        bpy.data.armatures,
    ):
        for item in list(collection):
            collection.remove(item, do_unlink=True)


def deselect_all():
    bpy.ops.object.select_all(action='DESELECT')


def set_active(obj):
    deselect_all()
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def apply_transforms(obj):
    """Apply location, rotation, and scale so verts are in world space."""
    set_active(obj)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def shade_smooth_obj(obj):
    for poly in obj.data.polygons:
        poly.use_smooth = True
    obj.data.update()


def assign_bone_group(obj, bone_name, weight=1.0):
    """Add every vertex in obj to a vertex group named after its controlling bone."""
    vg = obj.vertex_groups.new(name=bone_name)
    all_indices = list(range(len(obj.data.vertices)))
    vg.add(all_indices, weight, 'REPLACE')


# ──────────────────────────────────────────────────────────────────────────────
# MATERIAL FACTORY
# ──────────────────────────────────────────────────────────────────────────────

def make_clay_material(name, rgb, roughness=0.88):
    """
    Principled BSDF with a muted specular and slight subsurface scattering to
    mimic an air-dried clay / vinyl toy surface.
    Compatible with both Blender 3.x ('Subsurface') and 4.x ('Subsurface Weight').
    """
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")

    r, g, b = rgb
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness

    # Subsurface — key name changed between 3.x and 4.x
    for ss_key in ("Subsurface Weight", "Subsurface"):
        if ss_key in bsdf.inputs:
            bsdf.inputs[ss_key].default_value = 0.04
            break

    if "Subsurface Color" in bsdf.inputs:
        bsdf.inputs["Subsurface Color"].default_value = (r, g, b, 1.0)

    # Flatten the specular highlight
    for spec_key in ("Specular IOR Level", "Specular"):
        if spec_key in bsdf.inputs:
            bsdf.inputs[spec_key].default_value = 0.08
            break

    return mat


def build_materials():
    return {
        "white":  make_clay_material("Cat_White",  (0.95, 0.94, 0.90)),
        "black":  make_clay_material("Cat_Black",  (0.04, 0.04, 0.05)),
        "orange": make_clay_material("Cat_Orange", (0.84, 0.38, 0.07)),
        "pink":   make_clay_material("Cat_Pink",   (0.94, 0.60, 0.66)),
    }


# ──────────────────────────────────────────────────────────────────────────────
# PRIMITIVE HELPERS
# ──────────────────────────────────────────────────────────────────────────────

def make_sphere(name, loc, scale, mat, bone, segs=14, rings=10):
    """UV-sphere, smooth-shaded, single material, all verts in one vertex group."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segs, ring_count=rings, radius=1.0, location=loc
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    shade_smooth_obj(obj)
    assign_bone_group(obj, bone)
    return obj


def make_cone(name, loc, scale, rot, mat, bone, verts=8):
    """Cone, smooth-shaded, single material, all verts in one vertex group."""
    bpy.ops.mesh.primitive_cone_add(
        vertices=verts, radius1=1.0, depth=1.0,
        location=loc, rotation=rot
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    shade_smooth_obj(obj)
    assign_bone_group(obj, bone)
    return obj


# ──────────────────────────────────────────────────────────────────────────────
# CAT BODY PARTS
# ──────────────────────────────────────────────────────────────────────────────
#
# Coordinate conventions
#   +Y  = cat's nose / forward direction
#   +Z  = up
#   Paw pads rest near Z = 0.0
#   Body shoulder height ≈ 0.30 m  →  reasonable Unity scale (1 unit = 1 m)
#
# Part list drives both mesh placement and vertex group assignment.
# Each tuple: (factory_fn, name, loc, scale, rotation, material_key, bone_name)

def build_body_parts(M):
    parts = []
    R = math.radians

    # ── Torso ─────────────────────────────────────────────────────────────────
    parts.append(make_sphere("Body",         (0,     0.00, 0.26), (0.22, 0.30, 0.18), M["white"],  "Spine_02"))

    # Black saddle patch sitting on top of the torso (calico marking)
    parts.append(make_sphere("Patch_Saddle", (0,    -0.04, 0.41), (0.17, 0.22, 0.07), M["black"],  "Spine_02"))

    # Orange belly splash (calico third colour)
    parts.append(make_sphere("Patch_Belly",  (0,     0.09, 0.15), (0.11, 0.16, 0.06), M["orange"], "Spine_02"))

    # Neck connector between body and head
    parts.append(make_sphere("Neck",         (0,     0.24, 0.40), (0.08, 0.08, 0.10), M["white"],  "Neck"))

    # ── Head ──────────────────────────────────────────────────────────────────
    parts.append(make_sphere("Head",         (0,     0.36, 0.45), (0.17, 0.17, 0.16), M["white"],  "Head"))

    # Orange forehead stripe — classic calico face marking
    parts.append(make_sphere("Patch_Face",   (0.06,  0.42, 0.55), (0.08, 0.07, 0.07), M["orange"], "Head"))

    # Muzzle / snout pouch
    parts.append(make_sphere("Snout",        (0,     0.53, 0.43), (0.09, 0.07, 0.07), M["white"],  "Head"))

    # Nose
    parts.append(make_sphere("Nose",         (0,     0.57, 0.45), (0.025, 0.018, 0.018), M["pink"], "Head"))

    # Ears — orange like classic calico ear colouring
    parts.append(make_cone("Ear_L", (-0.09, 0.30, 0.58), (0.055, 0.04, 0.10),
                            (0.0, R(18),  0.0), M["orange"], "Head"))
    parts.append(make_cone("Ear_R", ( 0.09, 0.30, 0.58), (0.055, 0.04, 0.10),
                            (0.0, R(-18), 0.0), M["orange"], "Head"))

    # ── Front Legs (Hemingway / polydactyl — oversized paws) ──────────────────
    for side, sx in (("L", -1), ("R", 1)):
        fx = sx * 0.145

        # Upper arm — rounds into the shoulder
        parts.append(make_sphere(f"UpperArm_{side}",
                                  (fx,  0.24, 0.26), (0.068, 0.068, 0.12),
                                  M["white"], f"UpperArm_{side}"))

        # Forearm
        parts.append(make_sphere(f"ForeArm_{side}",
                                  (fx,  0.26, 0.12), (0.057, 0.057, 0.11),
                                  M["white"], f"ForeArm_{side}"))

        # ── Hemingway paw — noticeably wider than hind paws ───────────────────
        # The extra width and the extra toe digit are the polydactyl tell.
        parts.append(make_sphere(f"Paw_Front_{side}",
                                  (fx,  0.28, 0.038), (0.12, 0.15, 0.055),
                                  M["white"], f"Paw_Front_{side}"))

        # Three standard toe pods in a forward fan
        for ti, (ty, tx_off) in enumerate([
            (0.33,  0.00),   # centre toe
            (0.31, -0.055),  # inner toe
            (0.31,  0.055),  # outer toe
        ]):
            parts.append(make_sphere(f"Toe_Frt_{side}_{ti}",
                                      (fx + tx_off * sx, ty, 0.028),
                                      (0.036, 0.036, 0.026),
                                      M["white"], f"Paw_Front_{side}"))

        # Extra polydactyl digit — displaced medially, the Hemingway signature
        parts.append(make_sphere(f"Toe_Frt_{side}_Extra",
                                  (fx + sx * 0.085, 0.29, 0.028),
                                  (0.032, 0.032, 0.024),
                                  M["white"], f"Paw_Front_{side}"))

    # ── Back Legs ─────────────────────────────────────────────────────────────
    for side, sx in (("L", -1), ("R", 1)):
        bx = sx * 0.13

        # Thigh — black calico patch
        parts.append(make_sphere(f"Thigh_{side}",
                                  (bx, -0.19, 0.24), (0.08, 0.08, 0.14),
                                  M["black"], f"Thigh_{side}"))

        # Shin
        parts.append(make_sphere(f"Shin_{side}",
                                  (bx, -0.22, 0.10), (0.065, 0.065, 0.10),
                                  M["white"], f"Shin_{side}"))

        # Hind paw — smaller than front (normal cat anatomy)
        parts.append(make_sphere(f"Paw_Back_{side}",
                                  (bx, -0.25, 0.038), (0.09, 0.115, 0.05),
                                  M["white"], f"Paw_Back_{side}"))

        # Three standard hind toes
        for ti, (ty, tx_off) in enumerate([
            (-0.31,  0.00),
            (-0.30, -0.045),
            (-0.30,  0.045),
        ]):
            parts.append(make_sphere(f"Toe_Bck_{side}_{ti}",
                                      (bx + tx_off * sx, ty, 0.025),
                                      (0.032, 0.032, 0.022),
                                      M["white"], f"Paw_Back_{side}"))

    # ── Tail — black with a white tip ─────────────────────────────────────────
    parts.append(make_sphere("Tail_01",  (0, -0.34, 0.28), (0.055, 0.055, 0.09), M["black"], "Tail_01"))
    parts.append(make_sphere("Tail_02",  (0, -0.42, 0.39), (0.047, 0.047, 0.09), M["black"], "Tail_02"))
    parts.append(make_sphere("Tail_03",  (0, -0.46, 0.51), (0.040, 0.040, 0.08), M["black"], "Tail_03"))
    parts.append(make_sphere("Tail_Tip", (0, -0.47, 0.61), (0.037, 0.037, 0.050), M["white"], "Tail_03"))

    return parts


# ──────────────────────────────────────────────────────────────────────────────
# JOIN ALL PARTS INTO ONE SKINNABLE MESH
# ──────────────────────────────────────────────────────────────────────────────

def join_parts(parts):
    """
    Apply every part's transform (so vertices land in world space) then join
    into a single mesh object.  Vertex groups survive the join intact with
    remapped indices, giving us clean per-bone skinning regions.
    """
    # Apply transforms one by one; this bakes loc/rot/scale into vertex positions.
    for p in parts:
        apply_transforms(p)

    deselect_all()
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()

    joined = bpy.context.active_object
    joined.name = CAT_NAME
    joined.data.name = CAT_NAME + "_Mesh"
    return joined


# ──────────────────────────────────────────────────────────────────────────────
# ARMATURE RIG
# ──────────────────────────────────────────────────────────────────────────────
#
#   Root
#   └── Pelvis
#       ├── Spine_01
#       │   └── Spine_02
#       │       └── Chest
#       │           └── Neck
#       │               └── Head
#       │                   ├── Ear_L
#       │                   └── Ear_R
#       ├── Tail_01 ──► Tail_02 ──► Tail_03
#       ├── UpperArm_L ──► ForeArm_L ──► Paw_Front_L
#       ├── UpperArm_R ──► ForeArm_R ──► Paw_Front_R
#       ├── Thigh_L ──► Shin_L ──► Paw_Back_L
#       └── Thigh_R ──► Shin_R ──► Paw_Back_R

def build_armature():
    bpy.ops.object.add(type='ARMATURE', location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = CAT_NAME + "_Rig"
    arm_obj.show_in_front = True
    arm_data = arm_obj.data
    arm_data.name = CAT_NAME + "_Armature"

    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones

    def b(name, head, tail, parent=None, connect=False):
        """Create one edit-bone.  connect=True snaps head to parent's tail."""
        bone = eb.new(name)
        bone.head = head
        bone.tail = tail
        if parent:
            bone.parent = eb[parent]
            bone.use_connect = connect
        return bone

    # Root — single standing point; animator uses this to move the whole cat.
    b("Root",         (0,  0.00, 0.00), (0,  0.00, 0.08))

    # Pelvis — centre of mass anchor
    b("Pelvis",       (0,  0.00, 0.27), (0, -0.08, 0.30), "Root")

    # Spine chain (pelvis → chest), running forward and slightly upward
    b("Spine_01",     (0, -0.02, 0.28), (0,  0.06, 0.33), "Pelvis")
    b("Spine_02",     (0,  0.06, 0.33), (0,  0.16, 0.38), "Spine_01", connect=True)
    b("Chest",        (0,  0.16, 0.38), (0,  0.24, 0.42), "Spine_02", connect=True)
    b("Neck",         (0,  0.24, 0.42), (0,  0.32, 0.50), "Chest",    connect=True)
    b("Head",         (0,  0.32, 0.50), (0,  0.46, 0.51), "Neck",     connect=True)

    # Ears — short, diverging from head
    b("Ear_L",        (-0.09, 0.30, 0.52), (-0.10, 0.26, 0.64), "Head")
    b("Ear_R",        ( 0.09, 0.30, 0.52), ( 0.10, 0.26, 0.64), "Head")

    # Tail — three-segment curve lifting off the rump
    b("Tail_01",      (0, -0.30, 0.30), (0, -0.38, 0.36), "Pelvis")
    b("Tail_02",      (0, -0.38, 0.36), (0, -0.44, 0.46), "Tail_01", connect=True)
    b("Tail_03",      (0, -0.44, 0.46), (0, -0.47, 0.60), "Tail_02", connect=True)

    # Front left leg
    b("UpperArm_L",   (-0.145,  0.22, 0.32), (-0.145,  0.24, 0.19), "Chest")
    b("ForeArm_L",    (-0.145,  0.24, 0.19), (-0.145,  0.26, 0.07), "UpperArm_L", connect=True)
    b("Paw_Front_L",  (-0.145,  0.26, 0.07), (-0.145,  0.35, 0.03), "ForeArm_L",  connect=True)

    # Front right leg
    b("UpperArm_R",   ( 0.145,  0.22, 0.32), ( 0.145,  0.24, 0.19), "Chest")
    b("ForeArm_R",    ( 0.145,  0.24, 0.19), ( 0.145,  0.26, 0.07), "UpperArm_R", connect=True)
    b("Paw_Front_R",  ( 0.145,  0.26, 0.07), ( 0.145,  0.35, 0.03), "ForeArm_R",  connect=True)

    # Back left leg
    b("Thigh_L",      (-0.13, -0.14, 0.32), (-0.13, -0.21, 0.17), "Pelvis")
    b("Shin_L",       (-0.13, -0.21, 0.17), (-0.13, -0.23, 0.07), "Thigh_L",    connect=True)
    b("Paw_Back_L",   (-0.13, -0.23, 0.07), (-0.13, -0.31, 0.02), "Shin_L",     connect=True)

    # Back right leg
    b("Thigh_R",      ( 0.13, -0.14, 0.32), ( 0.13, -0.21, 0.17), "Pelvis")
    b("Shin_R",       ( 0.13, -0.21, 0.17), ( 0.13, -0.23, 0.07), "Thigh_R",    connect=True)
    b("Paw_Back_R",   ( 0.13, -0.23, 0.07), ( 0.13, -0.31, 0.02), "Shin_R",     connect=True)

    bpy.ops.object.mode_set(mode='OBJECT')
    return arm_obj


# ──────────────────────────────────────────────────────────────────────────────
# SKIN MESH TO ARMATURE
# ──────────────────────────────────────────────────────────────────────────────

def skin_mesh_to_armature(mesh_obj, arm_obj):
    """
    Parent the mesh to the armature and add an Armature modifier that reads
    the vertex groups built during part creation.
    """
    set_active(mesh_obj)
    mesh_obj.parent = arm_obj
    mesh_obj.parent_type = 'OBJECT'

    mod = mesh_obj.modifiers.new("Armature", 'ARMATURE')
    mod.object = arm_obj
    mod.use_vertex_groups = True


# ──────────────────────────────────────────────────────────────────────────────
# FBX EXPORT
# ──────────────────────────────────────────────────────────────────────────────

def export_fbx(mesh_obj, arm_obj):
    """
    Export settings target Unity:
      axis_forward = -Z, axis_up = Y  →  Unity's left-handed Z-forward space.
      apply_scale_options = FBX_SCALE_ALL  →  bakes all object scales.
      add_leaf_bones = False  →  Unity doesn't need Blender leaf bones.
      bake_anim = False  →  no animation data yet; reduces file size.
    """
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)

    deselect_all()
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj

    try:
        bpy.ops.export_scene.fbx(
            filepath=EXPORT_PATH,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL',
            bake_space_transform=False,
            object_types={'ARMATURE', 'MESH'},
            use_mesh_modifiers=True,
            mesh_smooth_type='FACE',
            add_leaf_bones=False,
            primary_bone_axis='Y',
            secondary_bone_axis='X',
            armature_nodetype='NULL',
            bake_anim=False,
            path_mode='COPY',
            axis_forward='-Z',
            axis_up='Y',
        )
        print(f"[HemingwayCat] ✓  FBX exported → {EXPORT_PATH}")
    except Exception as exc:
        print(f"[HemingwayCat] ✗  FBX export failed: {exc}")
        print("[HemingwayCat]    Try File → Export → FBX from the Blender menu.")


# ──────────────────────────────────────────────────────────────────────────────
# MAIN
# ──────────────────────────────────────────────────────────────────────────────

def main():
    print("[HemingwayCat] Clearing scene …")
    clear_scene()

    print("[HemingwayCat] Building materials …")
    M = build_materials()

    print("[HemingwayCat] Building body parts …")
    parts = build_body_parts(M)

    print(f"[HemingwayCat] Joining {len(parts)} parts into one mesh …")
    cat_mesh = join_parts(parts)

    print("[HemingwayCat] Building armature …")
    arm_obj = build_armature()

    print("[HemingwayCat] Skinning mesh to armature …")
    skin_mesh_to_armature(cat_mesh, arm_obj)

    print("[HemingwayCat] Exporting FBX …")
    export_fbx(cat_mesh, arm_obj)

    # Leave the armature selected and active for easy inspection
    set_active(arm_obj)
    print("[HemingwayCat] Done.  Select the armature and press A to see all bones.")


if __name__ == "__main__":
    main()
