# Stonehold Master Character: Archer (Draft)

This document establishes the Master Character specification using the Archer as the reference hero, setting the template for all future character models in Stonehold.

---

## 1. Character Biography & Fantasy
* **Role:** Single-target physical ranged damage dealer.
* **Fantasy:** A disciplined border scout from the stonehold keeps, using a heavy recurve bow to pick off priority targets.
* **Age Range:** 22 – 30 years.

## 2. Visual Proportions & Silhouette
* **Proportions:** Exaggerated stylized humanoid. 1:5 head-to-body ratio.
* **Silhouette Key Features:**
  * Oversized ranger hood that casts a deep shadow over the upper face.
  * Wide shoulders with asymmetrical armor pauldrons (larger on the left side to protect the bow-facing arm).
  * Long, thick bow extending past the head and feet when unstrung.
  * Solid, chunky boots that visually ground the character.

## 3. Armor Layering & Materials
* **Underlayer (Cloth):** Thick forest-green linen tunic, tightly bound.
* **Midlayer (Leather):** Hardened brown leather brigandine chest piece with steel stud reinforcements.
* **Outerlayer (Metal):** Left-shoulder steel pauldron, steel bracers on both wrists.
* **Emblem:** The Stonehold hammer-and-shield crest painted or embossed on the left pauldron.
* **Quiver:** Cylindrical leather quiver strapped diagonally across the back.

## 4. Bow & Quiver Design
* **Bow:** Stylized recurve bow constructed from dark yew wood with gold-plated iron tips. The center grip is wrapped in worn leather.
* **Arrows:** Red-feathered fletchings to increase visual visibility during projectile flight.

## 5. Color Palette & Wear
* **Colors:** Forest green (primary), leather brown (secondary), steel grey (accent), and gold (highlights).
* **Wear Level:** Moderate. Minor scuffs on leather, slight dirt on cloak hems, and light weathering on the bow tips.

## 6. Model & Rigging Requirements
* **Polygon Target:** ~1,800 triangles (LOD0).
* **Texture Target:** Shared 1024x1024 diffuse map with baked ambient occlusion and emissive accent map for magical archer variants.
* **LOD Requirements:**
  * LOD0: 1,800 triangles.
  * LOD1: 900 triangles (simplified hood, no quiver detail).
* **Rigging:** T-pose default, rigged using Unity Humanoid skeleton configuration. 
* **Sockets:**
  * Right Hand (Weapon attachment point).
  * Back Spine (Quiver attachment point).
* **VFX Attachment Points:**
  * `VFX_Muzzle` (At the center of the bow string).
  * `VFX_Feet` (For status/aura effects).

## 7. Scale, Orientation & Prohibited Details
* **Scale:** 1.0 unit scale. Exactly 1.8 meters tall in-engine.
* **Orientation:** Forward axis faces positive Z (+Z).
* **Prohibited Details:**
  * No individual hair strands or high-density beard meshes (use blocky geometry instead).
  * No loose belts, thin straps, or laces that cause pixel flickering.
  * No highly reflective chrome or metal shaders (use stylized diffuse highlights instead).

---

## 8. Continuity Rules for Future Heroes
All future heroes must conform to this Master template:
1. Maintain the 1:5 stylized head-to-body ratio.
2. Incorporate the Stonehold emblem on their primary shoulder or shield.
3. Feature at least one oversized visual accessory (e.g., hood, pauldron, weapon, cape) that defines their gameplay role from the top-down perspective.
4. Adhere to the single-pass URP Stylized Lit shader budget.
