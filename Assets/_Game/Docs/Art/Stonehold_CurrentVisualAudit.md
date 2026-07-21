# Stonehold Current Visual Audit

This document provides a detailed visual and technical audit of the current Stonehold art style, asset usage, and visual inconsistencies, establishing the baseline before producing final assets.

## 1. Hero Silhouette Readability
* **Current State:** Using Quaternius RPG Character Pack (Ranger, Warrior, Mage, etc.) as temporary CC0 placeholders.
* **Silhouette Analysis:** Proportions are cartoonish and modular. They are highly readable from close-up third-person views, but their details wash out under the high-angle, top-down portrait camera used in gameplay.
* **Issues:**
  * Similar heights and skeletal structures make them difficult to tell apart instantly from a top-down perspective.
  * Weapons (e.g., bows, staff) are small and lack the exaggerated scaling necessary for low-resolution mobile viewing.

## 2. Enemy Silhouette Readability
* **Current State:** Using five enemy classes (Grunt, Runner, Armored, Brute, Boss).
* **Silhouette Analysis:** They share similar humanoid structures with minor variations in armor or scale.
* **Issues:**
  * In dense waves, the distinction between a Grunt and a Runner is hard to detect, as their postures are virtually identical.
  * The Brute and Boss have larger scales but share the same base model posture, reducing the visual impact of elite units.

## 3. Castle Readability
* **Current State:** The castle/keep is represented by simple primitive mockups or basic placeholder meshes.
* **Silhouette Analysis:** Lacks presence, architectural personality, and clear visual cues for health/destruction states.
* **Issues:**
  * Does not feel like a majestic, defensive anchor for the screen.
  * The transition between the battlefield lane and the castle gate lacks clean visual grounding.

## 4. Combat Lane Readability
* **Current State:** Linear paths on the green terrain.
* **Readability Analysis:** Standard dirt textures on paths.
* **Issues:**
  * Contrast between the playable lanes and the surrounding dressing (rocks, trees) is weak.
  * Lack of clear lane borders or grid indicators makes tactical placement of defenses feel less precise.

## 5. Portrait-Mobile Composition
* **Current State:** The game camera is set up for portrait-mode aspect ratios (e.g., 9:16).
* **Composition Analysis:** Vertical space is ample, but horizontal space is heavily restricted.
* **Issues:**
  * Lane elements easily spill off-screen if the camera is too close.
  * The top-down angle reduces character models to mostly heads and shoulders, meaning helmets and shoulder pauldrons are the most visible parts of the characters. Proportions must adapt to this.

## 6. Lighting Consistency
* **Current State:** Flat, daylight direction lighting.
* **Lighting Analysis:** Simple directional light with default ambient settings.
* **Issues:**
  * Lack of contrast, depth, and mood.
  * No rim-lighting to pop characters off the dark background.
  * Shadows are often soft or missing on lower-end profiles, causing assets to look like they are floating.

## 7. Material and Texture Consistency
* **Current State:** Standard URP Lite shaders and custom flat materials.
* **Analysis:** Mismatched material properties between assets.
* **Issues:**
  * Quaternius models use standard flat diffuse textures, while environment assets from the "Mountain Terrain" pack use distinct stylized rock textures with high-contrast normals.
  * Shading differences cause heroes, enemies, and environment objects to look like they belong to three separate games.

## 8. Texture Density
* **Current State:** Varying texture resolutions.
* **Analysis:**
  * Some small environmental assets (like grass clumps) have disproportionately high texture resolutions compared to the main characters.
  * Character textures are optimized (low-res atlas) but lack localized ambient occlusion or detail map baking, resulting in a flat, unpainted appearance in-game.

## 9. Color Palette
* **Current State:** Bright green grass, brown paths, red/blue/grey characters.
* **Palette Analysis:** Unrestricted color usage.
* **Issues:**
  * No central hue dominance.
  * Enemy colors (reds, dark armor) clash with some hero colors (red fire mage), which can confuse the player regarding friend-vs-foe alignment at a glance.

## 10. VFX Readability
* **Current State:** Basic particle systems.
* **Readability Analysis:** Launch, projectile path, and impact effects are present but lack unified scale and color rules.
* **Issues:**
  * Fire, ice, and poison attacks share similar particle shapes and sizes, differing only by color tint.
  * Impact effects do not scale with damage output, and lingering effects (like poison or frost slows) lack strong visual cues on the affected enemies.

## 11. UI-to-World Visual Harmony
* **Current State:** Standard flat 2D canvas UI.
* **Harmony Analysis:** Basic card frames and placement circles.
* **Issues:**
  * UI looks like a temporary overlay rather than a premium, integrated part of the Stonehold fantasy universe.
  * Placement UI (valid/invalid indicators) does not match the stylized, magical theme of the combat.

## 12. Performance Suitability
* **Current State:** Low-poly models, but unoptimized draw calls.
* **Analysis:**
  * The assets themselves are low-poly, which is ideal for mobile.
  * However, they do not utilize material batching, meaning each character and enemy class incurs separate draw calls, which will severely bottleneck performance on mid-range Android devices during large waves.

---

## Conclusion & Recommendations
1. **Retain, Replace, or Recolor:**
   * **Retain:** The low-poly environment meshes (trees, rocks) from the nature kit as base models, but standardize their materials.
   * **Replace:** The temporary CC0 character models with bespoke, Meshy-generated models featuring exaggerated upper-body proportions (pauldrons, helmets, weapons) for portrait-angle readability.
   * **Recolor:** Standardize color rules: Warm/Earth tones for heroes/castle, Cold/Dark/Acid tones for enemies.
2. **Material Uniformity:** Transition all assets to a single URP Stylized Lit Shader that supports custom rim lighting and colored shadows.
