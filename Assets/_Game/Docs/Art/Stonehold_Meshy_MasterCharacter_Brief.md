# Stonehold Meshy Master Character Generation Brief

This document provides a copy-pasteable generation brief optimized for text-to-3D creation tool Meshy, specifically for generating the Master Character Archer and future heroes in a consistent stylized aesthetic.

---

## 1. Generation Parameters for Meshy (Text-to-3D)

### Prompt:
> "Stylized low poly 3D character, fantasy ranger archer, heroic proportions, wearing a large forest-green hood casting shadows on face, leather armor vest, large steel shoulder pauldron on left arm, thick brown leather boots. Heavy recurve bow in hand, leather quiver on back. Hand-painted textures, clean gradients, ambient occlusion, game-ready model. Cozy stylized fantasy art style, similar to World of Warcraft and Torchlight."

### Negative Prompt:
> "photorealistic, hyperrealistic, high-poly, complex textures, realistic face, thin limbs, noisy details, tiny weapons, multiple material slots, glitches, holes, double mesh, floating parts, modern clothes, sci-fi."

### Style Recommendation:
* **Art Style:** Stylized
* **Texture Style:** Hand-painted / Cartoon
* **Target Polygon Count:** Low-Poly (~2000 triangles)

---

## 2. Post-Generation Mesh Cleanup Procedures (Blender/Maya)

Before importing the generated mesh into Unity, it must undergo the following manual cleanup steps to ensure performance and quality:

1. **Polygon Reduction:**
   * Run a Decimate modifier to reduce any excessive triangle counts down to the 1,500 - 2,200 range.
   * Retain mesh density in the face/hood and hands while aggressively reducing the legs, back, and boots.

2. **UV & Atlas Packing:**
   * Remap UVs to fit onto a single square layout.
   * Repack texture maps into a single 1024x1024 diffuse map.

3. **Origin & Scale Reset:**
   * Set the pivot point (origin) to `(0, 0, 0)` at the center base between the character's feet.
   * Ensure scale is set to `1.0` and rotation is frozen so that the model faces positive Z (+Z).

4. **Rigging & Weighting:**
   * Bind the mesh to a standard Unity-compatible Humanoid skeleton.
   * Ensure bone weights do not exceed 2 influences per vertex for mobile optimization.
