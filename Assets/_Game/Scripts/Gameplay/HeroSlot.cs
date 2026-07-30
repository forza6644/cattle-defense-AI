using UnityEngine;
using UnityEngine.EventSystems;

namespace Stonehold
{
    public class HeroSlot : MonoBehaviour
    {
        public HeroDefinition startingHero;

        // How strongly hero identity color mixes over the prefab's own material colors
        // (1 = flat identity color, 0 = untouched prefab materials).
        private const float BodyTintBlend = 0.0f;
        private const string PresentationRootName = "HeroPresentation";

        private HeroAttack currentHero;

        public HeroAttack CurrentHero => currentHero;
        public bool IsOccupied => currentHero != null;

        private void Start()
        {
            // Create a small, thin slate pad under the slot for visual clarity
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "SlotPad_Visual";
            pad.transform.SetParent(transform);
            pad.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            pad.transform.localRotation = Quaternion.identity;
            pad.transform.localScale = new Vector3(1.1f, 0.05f, 1.1f);

            Renderer r = pad.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = new Color(0.2f, 0.25f, 0.3f);
            }

            HeroSelectionProxy selection = pad.AddComponent<HeroSelectionProxy>();
            selection.Configure(this);

            if (HeroRosterManager.Instance != null)
            {
                HeroRosterManager.Instance.RegisterSlot(this);
                return;
            }

            if (startingHero != null)
            {
                SpawnHero(startingHero);
            }
        }

        public bool SpawnHero(HeroDefinition hero)
        {
            if (hero == null || hero.heroPrefab == null || IsOccupied)
            {
                return false;
            }

            GameObject instance = Instantiate(hero.heroPrefab, transform.position, transform.rotation, transform);
            instance.name = hero.displayName + " Hero";
            // Legacy placeholders were authored much smaller than the art-adapter prefabs.
            // Keep the old compensation only for legacy visuals; normalize imported characters.
            instance.transform.localScale = instance.GetComponent<ArtAdapter>() != null
                ? Vector3.one * 0.95f
                : Vector3.one * 1.65f;

            Tower[] legacyTowers = instance.GetComponentsInChildren<Tower>();
            for (int i = 0; i < legacyTowers.Length; i++)
            {
                legacyTowers[i].enabled = false;
            }

            currentHero = instance.GetComponent<HeroAttack>();
            if (currentHero == null)
            {
                currentHero = instance.AddComponent<HeroAttack>();
            }

            SetHeroVisuals(hero, instance);

            currentHero.Configure(hero);
            HeroAbilityIndicator abilityIndicator = instance.GetComponent<HeroAbilityIndicator>();
            if (abilityIndicator == null)
            {
                abilityIndicator = instance.AddComponent<HeroAbilityIndicator>();
            }
            abilityIndicator.Configure(currentHero, GetHeroIdentityColor(hero.id));

            Collider[] heroColliders = instance.GetComponentsInChildren<Collider>();
            if (heroColliders.Length == 0)
            {
                heroColliders = new Collider[] { instance.AddComponent<CapsuleCollider>() };
            }

            for (int i = 0; i < heroColliders.Length; i++)
            {
                HeroSelectionProxy proxy = heroColliders[i].GetComponent<HeroSelectionProxy>();
                if (proxy == null)
                {
                    proxy = heroColliders[i].gameObject.AddComponent<HeroSelectionProxy>();
                }
                proxy.Configure(this);
            }
            return true;
        }

        private static Color GetHeroIdentityColor(string heroId)
        {
            switch (heroId)
            {
                case "archer": return new Color(0.45f, 0.9f, 0.3f);
                case "bombardier": return new Color(1f, 0.5f, 0.12f);
                case "frost_mage": return new Color(0.25f, 0.85f, 1f);
                case "fire_mage": return new Color(1f, 0.22f, 0.08f);
                case "electric_engineer": return new Color(1f, 0.92f, 0.12f);
                case "sniper": return new Color(0.75f, 0.4f, 1f);
                default: return Color.white;
            }
        }

        public void ClearHero()
        {
            if (currentHero == null)
            {
                return;
            }

            Destroy(currentHero.gameObject);
            currentHero = null;
        }

        /// <summary>
        /// Applies a distinct visual identity to each hero type: unique body color, accent,
        /// optional scale variation, and a small weapon prop for at-a-glance recognition.
        /// </summary>
        private void SetHeroVisuals(HeroDefinition hero, GameObject instance)
        {
            Color bodyColor;
            Color accentColor;
            Vector3 scaleMultiplier = Vector3.one;

            switch (hero.id)
            {
                case "archer":
                    bodyColor = new Color(0.25f, 0.45f, 0.2f);   // forest green
                    accentColor = new Color(0.5f, 0.35f, 0.2f);  // brown leather
                    break;
                case "bombardier":
                    bodyColor = new Color(0.3f, 0.3f, 0.32f);    // dark grey
                    accentColor = new Color(0.9f, 0.5f, 0.15f);  // orange
                    scaleMultiplier = new Vector3(1.05f, 1f, 1.05f);
                    break;
                case "frost_mage":
                    bodyColor = new Color(0.35f, 0.7f, 0.85f);   // cyan/ice
                    accentColor = new Color(0.85f, 0.92f, 1f);   // white-blue
                    break;
                case "fire_mage":
                    bodyColor = new Color(0.75f, 0.18f, 0.1f);   // deep red
                    accentColor = new Color(1f, 0.5f, 0.1f);     // ember orange
                    break;
                case "electric_engineer":
                    bodyColor = new Color(0.85f, 0.78f, 0.2f);   // yellow-gold
                    accentColor = new Color(0.3f, 0.3f, 0.35f);  // metallic dark
                    break;
                case "sniper":
                    bodyColor = new Color(0.35f, 0.2f, 0.55f);   // dark purple
                    accentColor = new Color(0.7f, 0.5f, 0.85f);  // lighter indigo
                    scaleMultiplier = new Vector3(0.85f, 1.1f, 0.85f);
                    break;
                default:
                    bodyColor = new Color(0.5f, 0.5f, 0.5f);
                    accentColor = Color.white;
                    break;
            }

            // Blend each renderer toward the hero identity color via MaterialPropertyBlock.
            // Blending (not replacing) keeps the prefab's per-part material variation
            // visible, and property blocks avoid creating material instances.
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            int baseColorId = Shader.PropertyToID("_BaseColor");
            foreach (Renderer rend in renderers)
            {
                Material shared = rend.sharedMaterial;
                Color matColor = shared != null && shared.HasProperty(baseColorId)
                    ? shared.GetColor(baseColorId)
                    : Color.white;
                rend.GetPropertyBlock(mpb);
                mpb.SetColor(baseColorId, Color.Lerp(matColor, bodyColor, BodyTintBlend));
                rend.SetPropertyBlock(mpb);
            }

            // Keep gameplay transforms, colliders, and projectile origins untouched. Imported
            // characters are normalized through their visual child only so every hero reads at
            // a similar scale on the wall without changing combat behaviour.
            NormalizeCharacterVisual(instance.transform, hero.id, scaleMultiplier);

            // Color the slot pad accent
            Transform padTransform = transform.Find("SlotPad_Visual");
            if (padTransform != null)
            {
                Renderer padRenderer = padTransform.GetComponent<Renderer>();
                if (padRenderer != null)
                {
                    padRenderer.GetPropertyBlock(mpb);
                    mpb.SetColor(baseColorId, accentColor * 0.5f);
                    padRenderer.SetPropertyBlock(mpb);
                }
            }

            // Hide default weapon meshes when the presentation replaces them.
            if (hero.id == "bombardier")
            {
                Transform sword = FindTransformRecursive(instance.transform, "Warrior_Sword");
                if (sword != null) sword.gameObject.SetActive(false);
            }
            else if (hero.id == "sniper")
            {
                Transform dagger = FindTransformRecursive(instance.transform, "Rogue_Dagger");
                if (dagger != null) dagger.gameObject.SetActive(false);
            }

            CreateHeroPresentation(hero.id, instance.transform, accentColor);
        }

        private static void NormalizeCharacterVisual(Transform heroRoot, string heroId, Vector3 profileScale)
        {
            Transform visualRoot = FindTransformRecursive(heroRoot, "QuaterniusVisual");
            if (visualRoot == null)
            {
                return;
            }

            // The portrait camera needs a little more body mass than the first pass,
            // but this remains a visual-child adjustment only.
            float normalizedScale = 1.03f;
            switch (heroId)
            {
                case "bombardier": normalizedScale = 1.08f; break;
                case "sniper": normalizedScale = 0.99f; break;
            }

            visualRoot.localScale = Vector3.Scale(visualRoot.localScale, profileScale * normalizedScale);
        }

        private void CreateHeroPresentation(string heroId, Transform heroRoot, Color accentColor)
        {
            Transform previous = heroRoot.Find(PresentationRootName);
            if (previous != null)
            {
                Destroy(previous.gameObject);
            }

            GameObject presentation = new GameObject(PresentationRootName);
            presentation.transform.SetParent(heroRoot);
            presentation.transform.localPosition = Vector3.zero;
            presentation.transform.localRotation = Quaternion.identity;
            presentation.transform.localScale = Vector3.one;

            // A restrained base ring supports recognition without competing with the
            // character silhouette at portrait-gameplay distance.
            CreatePresentationPiece(presentation.transform, "IdentityRing", PrimitiveType.Cylinder,
                new Vector3(0f, 0.018f, 0f), Vector3.one * 0.30f + new Vector3(0f, -0.275f, 0f),
                Quaternion.identity, Color.Lerp(new Color(0.16f, 0.18f, 0.22f), accentColor, 0.34f));

            switch (heroId)
            {
                case "archer":
                    CreateArcherPresentation(presentation.transform, accentColor);
                    break;
                case "bombardier":
                    CreateBombardierPresentation(presentation.transform, accentColor);
                    break;
                case "frost_mage":
                    CreateMagePresentation(presentation.transform, new Color(0.45f, 0.88f, 1f), "FrostStaff");
                    break;
                case "fire_mage":
                    CreateMagePresentation(presentation.transform, new Color(1f, 0.3f, 0.07f), "FireStaff");
                    break;
                case "electric_engineer":
                    CreateEngineerPresentation(presentation.transform, accentColor);
                    break;
                case "sniper":
                    CreateSniperPresentation(presentation.transform, accentColor);
                    break;
            }
        }

        private static void CreateArcherPresentation(Transform parent, Color accentColor)
        {
            CreatePresentationPiece(parent, "BowLimbUpper", PrimitiveType.Cube,
                new Vector3(0.25f, 0.68f, 0.18f), new Vector3(0.075f, 0.30f, 0.075f),
                Quaternion.Euler(0f, 0f, 26f), accentColor);
            CreatePresentationPiece(parent, "BowLimbLower", PrimitiveType.Cube,
                new Vector3(0.25f, 0.30f, 0.18f), new Vector3(0.075f, 0.30f, 0.075f),
                Quaternion.Euler(0f, 0f, -26f), accentColor);
            CreatePresentationPiece(parent, "BowString", PrimitiveType.Cube,
                new Vector3(0.37f, 0.49f, 0.18f), new Vector3(0.015f, 0.56f, 0.015f),
                Quaternion.identity, new Color(0.86f, 0.78f, 0.58f));
            CreatePresentationPiece(parent, "ArrowBundle", PrimitiveType.Cylinder,
                new Vector3(-0.20f, 0.39f, -0.10f), new Vector3(0.09f, 0.28f, 0.09f),
                Quaternion.Euler(0f, 0f, 16f), new Color(0.36f, 0.20f, 0.08f));
        }

        private static void CreateBombardierPresentation(Transform parent, Color accentColor)
        {
            CreatePresentationPiece(parent, "BombLauncherBody", PrimitiveType.Cube,
                new Vector3(0.29f, 0.48f, 0.18f), new Vector3(0.54f, 0.15f, 0.20f),
                Quaternion.Euler(0f, 10f, 0f), new Color(0.18f, 0.20f, 0.24f));
            CreatePresentationPiece(parent, "BombLauncherBarrel", PrimitiveType.Cylinder,
                new Vector3(0.51f, 0.50f, 0.18f), new Vector3(0.13f, 0.30f, 0.13f),
                Quaternion.Euler(0f, 0f, 82f), new Color(0.18f, 0.20f, 0.24f));
            CreatePresentationPiece(parent, "LauncherMuzzle", PrimitiveType.Cylinder,
                new Vector3(0.80f, 0.54f, 0.18f), new Vector3(0.16f, 0.04f, 0.16f),
                Quaternion.Euler(0f, 0f, 82f), accentColor);
            CreatePresentationPiece(parent, "BombSatchel", PrimitiveType.Sphere,
                new Vector3(-0.22f, 0.31f, -0.06f), Vector3.one * 0.18f,
                Quaternion.identity, new Color(0.10f, 0.11f, 0.13f));
        }

        private static void CreateMagePresentation(Transform parent, Color spellColor, string prefix)
        {
            CreatePresentationPiece(parent, prefix + "Shaft", PrimitiveType.Cylinder,
                new Vector3(0.23f, 0.47f, 0.16f), new Vector3(0.055f, 0.46f, 0.055f),
                Quaternion.Euler(0f, 0f, -12f), new Color(0.24f, 0.16f, 0.12f));
            CreatePresentationPiece(parent, prefix + "Focus", PrimitiveType.Sphere,
                new Vector3(0.33f, 0.90f, 0.16f), Vector3.one * 0.17f,
                Quaternion.identity, spellColor);
            CreatePresentationPiece(parent, prefix + "Rune", PrimitiveType.Cube,
                new Vector3(-0.18f, 0.50f, -0.12f), new Vector3(0.16f, 0.16f, 0.16f),
                Quaternion.Euler(45f, 45f, 45f), spellColor * 0.82f);
        }

        private static void CreateEngineerPresentation(Transform parent, Color accentColor)
        {
            CreatePresentationPiece(parent, "CoilPack", PrimitiveType.Cube,
                new Vector3(-0.16f, 0.47f, -0.12f), new Vector3(0.28f, 0.30f, 0.14f),
                Quaternion.identity, new Color(0.14f, 0.18f, 0.22f));
            CreatePresentationPiece(parent, "EmitterRail", PrimitiveType.Cube,
                new Vector3(0.22f, 0.53f, 0.17f), new Vector3(0.44f, 0.08f, 0.12f),
                Quaternion.Euler(0f, 8f, 0f), new Color(0.12f, 0.15f, 0.18f));
            CreatePresentationPiece(parent, "TeslaCoil", PrimitiveType.Cylinder,
                new Vector3(0.25f, 0.60f, 0.17f), new Vector3(0.09f, 0.29f, 0.09f),
                Quaternion.Euler(0f, 0f, -10f), accentColor);
            CreatePresentationPiece(parent, "CoilTip", PrimitiveType.Sphere,
                new Vector3(0.31f, 0.94f, 0.17f), Vector3.one * 0.12f,
                Quaternion.identity, new Color(1f, 0.94f, 0.34f));
        }

        private static void CreateSniperPresentation(Transform parent, Color accentColor)
        {
            CreatePresentationPiece(parent, "LongRifle", PrimitiveType.Cube,
                new Vector3(0.38f, 0.50f, 0.20f), new Vector3(0.82f, 0.08f, 0.10f),
                Quaternion.Euler(0f, 12f, 0f), new Color(0.12f, 0.13f, 0.16f));
            CreatePresentationPiece(parent, "RifleScope", PrimitiveType.Cylinder,
                new Vector3(0.28f, 0.61f, 0.20f), new Vector3(0.065f, 0.19f, 0.065f),
                Quaternion.Euler(0f, 0f, 90f), accentColor);
            CreatePresentationPiece(parent, "RifleMuzzle", PrimitiveType.Cylinder,
                new Vector3(0.88f, 0.50f, 0.20f), new Vector3(0.10f, 0.03f, 0.10f),
                Quaternion.Euler(0f, 0f, 90f), new Color(0.40f, 0.34f, 0.50f));
        }

        private static void CreatePresentationPiece(
            Transform parent,
            string pieceName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Color color)
        {
            GameObject piece = GameObject.CreatePrimitive(primitiveType);
            piece.name = pieceName;
            piece.transform.SetParent(parent);
            piece.transform.localPosition = localPosition;
            piece.transform.localRotation = localRotation;
            piece.transform.localScale = localScale;

            Collider collider = piece.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(Shader.PropertyToID("_BaseColor"), color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return parent;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindTransformRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }

    internal sealed class HeroSelectionProxy : MonoBehaviour, IPointerClickHandler
    {
        private HeroSlot slot;

        public void Configure(HeroSlot heroSlot) => slot = heroSlot;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (slot != null && slot.CurrentHero != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowHeroPanel(slot.CurrentHero);
            }
        }
    }
}
