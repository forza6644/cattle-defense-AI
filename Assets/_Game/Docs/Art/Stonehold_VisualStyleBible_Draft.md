# Stonehold Visual Style Bible (Draft)

This document establishes the official visual style guidelines and technical standards for Stonehold, ensuring a unified visual identity across all characters, environments, and UI.

---

## 1. Visual Identity & Target Audience
* **Game Visual Identity Statement:** Stonehold is a stylized, high-contrast, tactical portrait-mobile castle defense game where heavy stone defenses clash with corrupted magical waves.
* **Target Audience:** Mid-core mobile strategy players who appreciate tactical depth, clear readability, and clean, high-production stylized fantasy art.

## 2. Core Art Pillars
1. **Uncompromising Readability:** Gameplay state (hero placement, enemy classes, spell effects) must be immediately identifiable from a top-down portrait view in under 100 milliseconds.
2. **Defensive Weight:** Architectures and hero equipment feel heavy, solid, and structurally sound (dark stone, iron, thick wood).
3. **Corrupted Arcane Contrast:** The enemy threat is defined by cold, glowing, unstable arcane and toxic colors that contrast sharply with the warm, earth-toned defenses.

## 3. Shape Language & Silhouettes
* **Shape Language:**
  * **Heroes & Defenses:** Solid rectangles and triangles. Flat base foundations, heavy square shoulders, tapering upward.
  * **Enemies:** Sharp, jagged triangles and aggressive diamonds. Spikes, hunched postures, bent knees, and forward-reaching arms.
* **Silhouette Rules:** Characters must be identifiable by their silhouette alone. Weapons and headwear must be scaled up by 15-20% relative to standard human proportions to stand out from the top-down camera view.
* **Proportions:**
  * **Characters:** Stylized 1:5 head-to-body ratio. Broad chests, large hands/forearms, and heavy boots.
  * **Castle & Defenses:** Exaggerated stone block sizes (thick mortar lines), oversized doors, and thick wooden gates.

## 4. Environment & Materials
* **Environment Rules:** Environmental decoration (trees, rocks) must frame the lanes but remain 20% lower in saturation than the playable lane and characters to prevent visual noise.
* **Material Rules:** Single-pass Stylized Lit shaders. No high-frequency specular highlights. Use soft, metallic-roughness curves.
* **Texture Rules:** Hand-painted stylized look. Use clean gradients, highlighted edges, and baked ambient occlusion. Avoid photo-textures.

## 5. Lighting & Color Palette
* **Lighting Rules:**
  * Directional Warm Key (Yellow/Amber, Intensity 1.2, 45-degree pitch).
  * Cool Ambient Sky Fill (Soft Blue/Grey, Intensity 0.4).
  * Cool Rim Light (Light Cyan, Intensity 1.0) applied to character shaders to separate them from the ground.
* **Primary Palette:** Desaturated earth browns, dark granite greys, and olive greens.
* **Secondary Palette:** Gold/Flame orange (for hero abilities) and cold neon blue/purple (for enemy spells).
* **Color Coding:**
  * **Heroes:** Warm tones (gold, red, bronze, forest green).
  * **Enemies:** Cold/unnatural tones (neon purple, acid green, pale blue).
  * **Damage Types:**
    * Fire: Red-orange
    * Frost: Sky blue
    * Electric: Bright violet
    * Poison/Acid: Lime green

## 6. VFX & UI Principles
* **VFX Sequence Rules:**
  * *Launch:* Bright flash at the source.
  * *Path:* Clean, opaque trail (no thin wispy lines).
  * *Impact:* Concentrated splash of light, scaling with damage.
  * *Lingering:* Distinct color overlay on the target model (e.g., frozen targets turn icy-blue; poisoned targets glow acid-green).
* **UI Principles:** High-contrast frames, clear spacing, and modern typography. 
* **Portrait Safe-Area Principles:** All critical interactive HUD elements (wave progress, cards, gold counters) must sit within the computed `Screen.safeArea` bounds to prevent notches and screen corners from cutting off UI.

## 7. Mobile Performance Budget & Constraints
* **Polygon Targets:**
  * Main Heroes: 1,500 – 2,200 triangles.
  * Enemies: 800 – 1,200 triangles.
  * Environment Props: 100 – 500 triangles.
* **LOD Rules:**
  * LOD0 (Gameplay View): 100% polygons.
  * LOD1 (Distant/Offscreen): 50% polygons, disable dynamic bones or accessories.
* **Texture Size Guidelines:**
  * Characters: Single shared 1024x1024 atlas texture per group (e.g., one atlas for all enemies).
  * Environment: 512x512 tileable maps.
* **Material-Count Guidelines:** Maximum 1 material slot per character mesh.
* **Particle-Count Guidelines:** Maximum 30 active particles per standard projectile impact. No screen-space refraction shaders on mobile.

## 8. Forbidden Style Combinations
* No photorealistic textures on stylized low-poly meshes.
* No thin, high-frequency line details that create moiré patterns on mobile screens.
* No warm red/gold colors on enemy designs (reserved exclusively for friendly units and fire magic).
* No screen-space post-processing bloom exceeding 0.5 intensity on mobile build targets.

## 9. Asset Acceptance & Unity Import Checklist
* [ ] Mesh origin set to ground level (0, 0, 0) between the character's feet.
* [ ] Forward axis faces positive Z (+Z).
* [ ] All textures imported as ASTC 6x6 (or compressed format) with MIP maps enabled.
* [ ] Rigging uses Unity Humanoid Avatar where applicable.
* [ ] No unused material slots or empty texture references.
* [ ] Meshy-generated assets must go through a manual mesh cleanup and polygon reduction pass before project import.
