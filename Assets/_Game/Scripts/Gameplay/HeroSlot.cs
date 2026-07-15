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
            // The portrait camera is pulled back to show the complete battlefield.
            // Compensate here so each defender still reads clearly above the wall.
            instance.transform.localScale = Vector3.one * 2.1f;

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

            // Apply scale variation
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scaleMultiplier);

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

            // Hide default weapon meshes for Bombardier and Sniper
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

            // Add a small weapon prop for visual identification
            CreateWeaponProp(hero.id, instance.transform, accentColor);
        }

        private void CreateWeaponProp(string heroId, Transform parent, Color color)
        {
            PrimitiveType shape;
            Vector3 localPos;
            Vector3 localScale;
            Quaternion localRot = Quaternion.identity;

            switch (heroId)
            {
                case "archer":
                    shape = PrimitiveType.Cylinder;  // quiver
                    localPos = new Vector3(-0.25f, 0.35f, -0.15f);
                    localScale = new Vector3(0.08f, 0.25f, 0.08f);
                    localRot = Quaternion.Euler(0f, 0f, 15f);
                    break;
                case "bombardier":
                    shape = PrimitiveType.Sphere;    // bomb
                    localPos = new Vector3(0.3f, 0.1f, 0f);
                    localScale = new Vector3(0.22f, 0.22f, 0.22f);
                    break;
                case "frost_mage":
                    shape = PrimitiveType.Cube;      // crystal
                    localPos = new Vector3(0.25f, 0.4f, 0f);
                    localScale = new Vector3(0.1f, 0.15f, 0.1f);
                    localRot = Quaternion.Euler(0f, 45f, 45f);
                    break;
                case "fire_mage":
                    shape = PrimitiveType.Sphere;    // glowing ember
                    localPos = new Vector3(0.25f, 0.45f, 0f);
                    localScale = new Vector3(0.14f, 0.14f, 0.14f);
                    break;
                case "electric_engineer":
                    shape = PrimitiveType.Cylinder;  // tesla coil
                    localPos = new Vector3(0f, 0.65f, 0f);
                    localScale = new Vector3(0.06f, 0.18f, 0.06f);
                    break;
                case "sniper":
                    shape = PrimitiveType.Cylinder;  // rifle barrel
                    localPos = new Vector3(0.35f, 0.3f, 0f);
                    localScale = new Vector3(0.04f, 0.3f, 0.04f);
                    localRot = Quaternion.Euler(0f, 0f, 90f);
                    break;
                default:
                    return;
            }

            Transform propParent = parent;
            if (heroId == "bombardier" || heroId == "sniper")
            {
                Transform weaponMount = FindTransformRecursive(parent, "Weapon.R");
                if (weaponMount != null)
                {
                    propParent = weaponMount;
                    if (heroId == "bombardier")
                    {
                        localPos = new Vector3(0f, 0f, 0f);
                    }
                    else if (heroId == "sniper")
                    {
                        localPos = new Vector3(0f, 0.15f, 0f);
                        localRot = Quaternion.Euler(0f, 0f, 90f);
                        localScale = new Vector3(0.04f, 0.35f, 0.04f);
                    }
                }
            }

            GameObject prop = GameObject.CreatePrimitive(shape);
            prop.name = "WeaponProp";
            prop.transform.SetParent(propParent);
            prop.transform.localPosition = localPos;
            prop.transform.localRotation = localRot;
            prop.transform.localScale = localScale;

            // Remove collider from prop so it doesn't interfere with gameplay
            Collider propCollider = prop.GetComponent<Collider>();
            if (propCollider != null) Destroy(propCollider);

            Renderer propRenderer = prop.GetComponent<Renderer>();
            if (propRenderer != null)
            {
                // Property block only - no material instances for temporary props.
                // Fire Mage's ember just uses a hotter color instead of true emission.
                Color propColor = heroId == "fire_mage" ? new Color(1f, 0.45f, 0.08f) : color;
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                mpb.SetColor(Shader.PropertyToID("_BaseColor"), propColor);
                propRenderer.SetPropertyBlock(mpb);
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
