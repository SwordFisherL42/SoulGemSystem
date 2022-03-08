using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace SoulGemSystem
{
    public class SoulGem : MonoBehaviour
    {
        public Item item;
        public Collider collider;
        public MeshRenderer renderer;
        public ParticleSystem particle;
        public AudioSource audio;
        public AudioClip originalClip;
        public float energy;
        List<AudioClip> audioClips;
        SoulGemModule module;
        GemActionType type;
        GemAction mainAction;
        GemAction gemEffect;
        Animator animator;
        Color gemColor;
        Color gemEmpty;
        bool isCharged;
        bool isThrown;
        string soulName;
        float emissionIntensityHDR;
        float minThrowVelocity;

        void Awake()
        {
            // BaS Custom References
            item = GetComponent<Item>();
            module = item.data.GetModule<SoulGemModule>();
            animator = item.GetCustomReference(module.gemAnimator).GetComponent<Animator>();
            renderer = item.GetCustomReference(module.gemRenderer).GetComponent<MeshRenderer>();
            collider = item.GetCustomReference(module.gemCollider).GetComponent<Collider>();
            particle = item.GetCustomReference(module.gemParticle).GetComponent<ParticleSystem>();
            audio = item.GetCustomReference(module.gemAudio).GetComponent<AudioSource>();
            originalClip = audio.clip;
            // Gem Setup
            minThrowVelocity = SoulGemLevelModule.settings.minThrowVelocity;
            emissionIntensityHDR = SoulGemLevelModule.settings.glowIntensity;
            renderer.material = new Material(renderer.material);
            renderer.enabled = true;
            animator.enabled = false;
            SetGemType(module.parsedGemType);
            // Gem Events
            mainAction = SoulGemLibrary.GenerateAction(type);
            gemEffect = new GemAction(SoulGemLibrary.CrystalEffect);
            item.OnHeldActionEvent += OnHeldActionEvent;
            item.OnTelekinesisGrabEvent += TKGrabbed;
            item.OnGrabEvent += Grabbed;
            item.OnUngrabEvent += UnGrabbed; 
        }

        void OnDestory()
        {
            item.OnHeldActionEvent -= OnHeldActionEvent;
            item.OnTelekinesisGrabEvent -= TKGrabbed;
            item.OnGrabEvent -= Grabbed;
            item.OnUngrabEvent -= UnGrabbed;
        }

        void OnCollisionEnter(Collision hit)
        {
            if (!isCharged) return;
            SoulWeapon hitItem = hit.transform.root.GetComponent<SoulWeapon>();
            if (hitItem == null)
            {
                if (isThrown)
                {
                    mainAction(this, module, transform);
                    isThrown = false;
                }
                // TODO: check if regular weapon has collision while held => imbue item
                return;
            }
            ChargeSoulWeapon(hitItem);
        }

        void OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action button)
        {
            if (button == Interactable.Action.AlternateUseStart)
            {
                if (isCharged)
                {
                    PlayerControl.handRight.HapticShort(module.hapticForce);
                    mainAction(this, module, transform, button);
                }
            }
        }

        void TKGrabbed(Handle handle, SpellTelekinesis teleGrabber)
        {
            SetAnimatorState(state: false);
        }

        void Grabbed(Handle handle, RagdollHand ragdollHand)
        {
            SetAnimatorState(state: false);
        }

        void UnGrabbed(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (!throwing || handle.rb.velocity.magnitude < minThrowVelocity) return;
            isThrown = true;
        }

        void SetAnimatorState(bool state)
        {
            if (animator == null) return;
            item.rb.isKinematic = state;
            item.rb.useGravity = !state;
            animator.enabled = state;
            collider.enabled = !state;
            if (!state)
            {
                Destroy(animator);
                renderer.transform.localPosition = Vector3.zero;
                renderer.transform.localRotation = Quaternion.identity;
            }
        }

        void ChargeSoulWeapon(SoulWeapon i, bool release = false)
        {
            i.ChargeWeapon();
            SetCharge(state: false);
            if (release) item.handles[0].Release();
            gemEffect(this, module, transform);
        }

        public void DroppedFromCreature()
        {
            gemEffect(this, module, transform);
            SetAnimatorState(state: true);
            SetCharge(state: true, init: true);
        }

        public void SetCharge(bool state, bool init = false)
        {
            if (isCharged == state && !init) return;
            isCharged = state;
            if (isCharged)
            {
                if (module.usesCustomShader)
                {
                    renderer.material.SetInt(SoulGemLibrary.shaderEmissionBool, 1);
                    renderer.material.SetColor(SoulGemLibrary.shaderBaseColor, gemColor);
                    renderer.material.SetVector(SoulGemLibrary.shaderEmissionColor, gemColor * emissionIntensityHDR);
                }
                else
                {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.color = gemColor;
                }
            }
            else
            {
                if (module.usesCustomShader)
                {
                    renderer.material.SetInt(SoulGemLibrary.shaderEmissionBool, 0);
                    renderer.material.SetColor(SoulGemLibrary.shaderBaseColor, gemEmpty);
                }
                else
                {
                    renderer.material.DisableKeyword("_EMISSION");
                    renderer.material.color = gemEmpty;
                }
                item.data.displayName = $"{item.data.displayName} (Empty)";
            }
        }

        public GemActionType GetGemType() { return type; }

        public void SetGemType(GemActionType newType)
        {
            type = newType;
            mainAction = SoulGemLibrary.GenerateAction(type);
            energy = SoulGemLevelModule.settings.gemSettings[type].charge;
            gemColor = SoulGemLevelModule.settings.useCustomRGB ? SoulGemLevelModule.settings.gemSettings[type].GetColor() : SoulGemLibrary.GenerateColor(type);
            gemEmpty = SoulGemLevelModule.settings.useCustomRGB ? SoulGemLevelModule.settings.gemSettings[type].GetEmptyColor() : SoulGemLibrary.gemEmptyDefault;
            var psmain = particle.main;
            psmain.startColor = gemColor;
        }

        public void SetRenderMaterial(Material m) { renderer.material = new Material(m); }

        public void SetGemColor(Color c) { gemColor = c; }

        public void SetName(string name)
        {
            soulName = name;
            item.data.displayName = SoulNames.ParseItemName(name);
        }

        public string GetSoulName() { return string.IsNullOrEmpty(soulName) ? "Null" : soulName; }

        public void SetAudioClips(List<AudioClip> newClipList) { audioClips = newClipList; }

        public void PlayRandomClip()
        {
            if (audioClips == null || audioClips.Count == 0) return;
            int i = SoulGemLevelModule.local.random.Next(audioClips.Count);
            audio.clip = audioClips[i];
            audio.Play();
        }

        public bool GetCharge() { return isCharged; }

        public List<AudioClip> AudioClips() { return audioClips; }
    }
}