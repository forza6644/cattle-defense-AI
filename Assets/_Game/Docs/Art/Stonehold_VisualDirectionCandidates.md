# Stonehold Visual Direction Candidates

This document defines three professional visual-direction candidates for Stonehold and proposes a recommended direction for final asset production.

---

## Candidate A: STYLIZED DARK STONE FANTASY (Recommended)

### 1. Palette & Shading
* **Primary Palette:** Warm iron ores, dark granite greys, weathered basalt.
* **Secondary Palette:** Golden flame yellows, torch oranges, cold ice blues (for magical elements).
* **Neutral Palette:** Desaturated earth browns, deep shadows.
* **Lighting:** High-contrast daylight with strong shadow cast. Directional warm key light, cool sky fill light, sharp rim lights on characters.

### 2. Mesh & Proportions
* **Character Proportions:** Chunky, heroic proportions (1:5 head-to-body ratio). Oversized helmets, shoulder guards (pauldrons), and boots.
* **Hero Silhouette Language:** Stout, angular, defensive postures. Heavy shields, thick cloaks, clear weapon outlines (large bow arms for Archer).
* **Enemy Silhouette Language:** Aggressive, forward-leaning postures. Jagged spikes, heavy hunched shoulders, elongated weapons.
* **Castle Language:** Thick, blocky granite walls with heavy crenellations, heavy iron reinforcement bands, and vertical banners.
* **Weapon Language:** Oversized, thick blades and bows. Emphasized silhouettes that do not wash out at small scales.

### 3. VFX & UI Style
* **VFX Language:** High-contrast particles. Sparking orange fire, crisp blue electric arcs, thick glowing trails.
* **UI Language:** Dark stone frames with gold metallic trims, clean high-contrast serif typography (e.g., Outfit or Inter).
* **Camera Presentation:** High-angle top-down tilt (35–45 degrees) with ortho-like perspective to maintain lane clarity.

### 4. Technical & Production Assessment
* **Mobile Readability:** Excellent. High contrast between characters and the environment prevents unit blending.
* **Performance Cost:** Low-to-Medium. Relies on clean geometry and simplified textures rather than heavy shaders. Supports static light baking on terrain.
* **Asset Reuse Potential:** High. Standardized dark stone textures can be reused across all environment assets and enemy armor.
* **Main Advantages:**
  * Fits the defensive, sieged atmosphere of Stonehold.
  * Excellent readability on mobile devices even under low screen brightness.
  * Simple, strong shapes are ideal for Meshy's AI generation pipelines.
* **Main Risks:**
  * If color saturation is too low, the game could look muddy. Warm/cold accent colors must be enforced strictly.

---

## Candidate B: BRIGHT HEROIC FANTASY

### 1. Palette & Shading
* **Primary Palette:** Light sandstones, warm oak woods, bright sky blues.
* **Secondary Palette:** Royal purples, grassy greens, bright crimson reds.
* **Neutral Palette:** Warm off-whites, light pebble greys.
* **Lighting:** Bright, direct mid-day sun. Warm, soft shadows, high ambient light, low-intensity rim lights.

### 2. Mesh & Proportions
* **Character Proportions:** Classic stylized fantasy proportions (1:6 head-to-body ratio). Softer, rounded shapes.
* **Hero Silhouette Language:** Graceful, upright, confident postures. Flowing capes, round shields.
* **Enemy Silhouette Language:** Traditional goblin/orc proportions (squat, rounded, green-skinned or leather-clad).
* **Castle Language:** Clean limestone and timber structures, blue-tiled roofs, bright colored banners.
* **Weapon Language:** Smooth, polished silver blades, polished wood bows.

### 3. VFX & UI Style
* **VFX Language:** Soft, magical glow effects. Golden sparkles, bright blue water/wind swirls.
* **UI Language:** Clean parchment and light wood textures with bronze fittings, modern sans-serif typography.
* **Camera Presentation:** Traditional tilted-down angle with soft depth-of-field.

### 4. Technical & Production Assessment
* **Mobile Readability:** Medium. Bright characters can easily blend into a bright grass environment if terrain saturation is not controlled.
* **Performance Cost:** Low. Standard diffuse and basic lightmaps work perfectly.
* **Asset Reuse Potential:** Medium. Requires distinct textures for wood, sandstone, and various cloths.
* **Main Advantages:**
  * Broad, family-friendly commercial appeal.
  * Inviting and highly accessible aesthetic.
* **Main Risks:**
  * Lacks the tense, high-stakes defense identity of "holding the stone".
  * Readability of spell effects can be washed out by the bright ambient environment.

---

## Candidate C: ARCANE NIGHT SIEGE

### 1. Palette & Shading
* **Primary Palette:** Deep obsidian blacks, midnight purples, shadow blues.
* **Secondary Palette:** Neon greens (poison/acid), electric magentas, glowing cyan.
* **Neutral Palette:** Charcoal greys, cold mist white.
* **Lighting:** Night scene. Moonlight directional light (deep blue key), bright colored local point lights (torches, magical crystals, glowing runes) serving as local key sources.

### 2. Mesh & Proportions
* **Character Proportions:** Tall, slender, ethereal proportions (1:7 head-to-body ratio).
* **Hero Silhouette Language:** Cloaked, mysterious, magical. Glowing crystal weapons.
* **Enemy Silhouette Language:** Monstrous, shadow-like, with glowing eyes and veins.
* **Castle Language:** Sleek, sharp dark spire architecture with embedded glowing runes.
* **Weapon Language:** Runed blades, glowing crystal bows, floating magic orbs.

### 3. VFX & UI Style
* **VFX Language:** Bright neon emissions, trails, and portal-like particles. High bloom intensity.
* **UI Language:** Translucent dark glassmorphic panels with cyan/magenta neon trims.
* **Camera Presentation:** Night-vision style or highly contrasting spotlight camera.

### 4. Technical & Production Assessment
* **Mobile Readability:** Medium-to-High (due to high contrast emissive elements against dark ground), but can strain eyes during long sessions.
* **Performance Cost:** High. Requires emissive materials, dynamic lights (or high-quality light baking for static lights), and post-processing bloom.
* **Asset Reuse Potential:** Low. Requires specialized shaders and emissive map bakes.
* **Main Advantages:**
  * Highly unique, striking roguelite aesthetic.
  * VFX and magical attacks look extremely satisfying.
* **Main Risks:**
  * Significant performance overhead for low-end mobile devices due to fill-rate bottlenecks from glowing particles and post-processing.
  * Night environments make non-glowing characters and standard lanes hard to distinguish.

---

## Recommended Direction: Candidate A (STYLIZED DARK STONE FANTASY)

### Rationale:
1. **Gameplay Synergy:** Stonehold's gameplay is a tactical castle defense game. The "Dark Stone Fantasy" style communicates high stakes, solid defense, and heavy military resistance.
2. **Mobile Readability:** Warm hero colors and cold enemy magic contrast sharply with the desaturated dark stone/basalt lanes and environment, allowing instant tactical awareness in portrait mode.
3. **Meshy Generation Compatibility:** Meshy's AI generation handles simple, chunky, geometric shapes (like heavy shoulder pauldrons, thick helmets, and blocky weapons) far better than slender, detailed, or soft-rounded characters.
4. **Performance:** Darker, static environments allow high-quality baked lightmaps while characters can use simple URP Lit materials with custom rim light, keeping draw calls and shadow calculations low.

*Note: This recommendation remains a draft pending user approval.*
