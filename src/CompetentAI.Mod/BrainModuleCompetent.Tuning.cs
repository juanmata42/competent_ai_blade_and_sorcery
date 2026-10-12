using System;
using CompetentAI.Core;
using ThunderRoad;
using UnityEngine;

namespace CompetentAI.Mod
{
    internal sealed partial class BrainModuleCompetent
    {
        private struct Originals
        {
            public Vector2 attackInterval;
            public float animSpeed;
            public Vector2 riposteChance;
            public Vector2 riposteDelay;
            public float parryAdjust;
            public float aboutToBeHit;
            public AnimationCurve dodgeCurve;
            public bool oneHandIK;
            public float oneHandPos;
            public float oneHandRot;
            public bool sideDodge;
            public bool parryEnabled;
            public float rangeDelta;
            public Vector2 dodgeCooldown;
            public float recoilMin;
        }

        private Originals _orig;
        private bool _origCaptured;
        private bool _tuned;
        private bool _tunedIk;
        private bool _tunedSide;
        private float _baseAdjust;
        private bool _ikEnabledByMe;
        private bool _ikFailed; // persistent: never retried for this creature, and not cleared by RestoreTuning

        /// <summary>Applies or removes the archetype tuning according to the current effective flags.</summary>
        private void SyncTuning(bool first)
        {
            EffectiveFlags f = Runtime.Flags;
            if (f.SkillLevel != _appliedLevel || f.LevelShift != _appliedShift)
            {
                string before = _profileName;
                RestoreTuning();      // re-applied below with the new profile
                ChooseProfile();
                if (before != _profileName) Log.Info($"{_tag} skill level {before} -> {_profileName} (menu)");
            }
            bool wantTuning = f.Enabled && f.ProfileTuning;
            bool wantIk = wantTuning && f.OneHandIK && _profile.OneHandIK && !_ikFailed;
            bool wantSide = wantTuning && f.SideDodge && _profile.SideDodge;

            if (_tuned && (!wantTuning || wantIk != _tunedIk || wantSide != _tunedSide))
                RestoreTuning();
            if (!_tuned && wantTuning)
                ApplyTuning(wantIk, wantSide);

            // Weapon placement IK can only be switched on once the stock module has built its anchors (first defence).
            if (_tuned && _tunedIk && !_ikEnabledByMe) TryEnableIk();

            if (!f.Enabled || !f.Variety) RestoreWeights();
        }

        private void CaptureOriginals()
        {
            if (_origCaptured) return;
            _orig = new Originals
            {
                attackInterval = _melee.minMaxTimeBetweenAttack,
                animSpeed = _melee.animationSpeedMultiplier,
                riposteChance = _melee.recoilNothingRiposteChance,
                riposteDelay = _melee.minMaxRiposteDelay,
                parryAdjust = _defense.parryAdjustSpeed,
                aboutToBeHit = _defense.aboutToBeHitTiming,
                dodgeCurve = _defense.dodgeChanceCurve,
                oneHandIK = _defense.oneHandUsesIK,
                oneHandPos = _defense.oneHandMaxIKWeightPos,
                oneHandRot = _defense.oneHandMaxIKWeightRot,
                sideDodge = _defense.sideDodgeEnabled,
                parryEnabled = _defense.parryEnabled,
                rangeDelta = _melee.maxRangeDelta,
                dodgeCooldown = _defense.minMaxDodgeCooldown,
                recoilMin = _melee.recoilMinWeaponSpeed
            };
            _origCaptured = true;
        }

        private void ApplyTuning(bool ik, bool side)
        {
            CaptureOriginals();
            SkillProfile p = _profile;

            Vector2 iv = _orig.attackInterval;
            float ix = iv.x * p.AttackIntervalMul, iy = iv.y * p.AttackIntervalMul;
            if (p.AttackIntervalCapMax > 0f)
            {
                ix = Mathf.Min(ix, p.AttackIntervalCapMin);
                iy = Mathf.Min(iy, p.AttackIntervalCapMax);
                if (iy < ix) iy = ix;
            }
            _melee.minMaxTimeBetweenAttack = new Vector2(ix, iy);
            _melee.maxRangeDelta = Mathf.Min(0.55f, _orig.rangeDelta * p.RangeDeltaMul);
            _melee.animationSpeedMultiplier = _orig.animSpeed * p.AnimSpeedMul;

            Vector2 rc = _orig.riposteChance;
            _melee.recoilNothingRiposteChance = new Vector2(Mathf.Clamp01(rc.x - p.RiposteChanceShift), Mathf.Clamp01(rc.y - p.RiposteChanceShift));
            Vector2 rd = _orig.riposteDelay;
            _melee.minMaxRiposteDelay = new Vector2(rd.x * p.RiposteDelayMul, rd.y * p.RiposteDelayMul);

            _baseAdjust = Mathf.Max(_orig.parryAdjust * p.ParryAdjustSpeedMul, p.ParryAdjustSpeedFloor);
            _defense.minMaxDodgeCooldown = _orig.dodgeCooldown * p.DodgeCooldownMul;
            _defense.parryAdjustSpeed = _baseAdjust;
            _defense.aboutToBeHitTiming = _orig.aboutToBeHit * p.AboutToBeHitMul;
            if (_orig.dodgeCurve != null && Mathf.Abs(p.DodgeChanceMul - 1f) > 0.01f)
                _defense.dodgeChanceCurve = ScaleCurve(_orig.dodgeCurve, p.DodgeChanceMul);

            // oneHandUsesIK is NOT set here: while it is true the stock HeldItemsChange calls EnableIK on every weapon
            // change, which dereferences anchors that only exist after the first defence. TryEnableIk flips it later.
            if (side) _defense.sideDodgeEnabled = true;

            _tuned = true;
            _tunedIk = ik;
            _tunedSide = side;
            Log.Debug_($"{_tag} tuning applied profile={_profileName} adjust={_defense.parryAdjustSpeed:0.0} " +
                       $"aboutToBeHit={_defense.aboutToBeHitTiming:0.00} interval=({_melee.minMaxTimeBetweenAttack.x:0.00},{_melee.minMaxTimeBetweenAttack.y:0.00}) " +
                       $"riposte=({_melee.recoilNothingRiposteChance.x:0.00},{_melee.recoilNothingRiposteChance.y:0.00}) ik={ik} side={side}");
        }

        private void RestoreTuning()
        {
            if (!_tuned || !_origCaptured) return;
            _melee.minMaxTimeBetweenAttack = _orig.attackInterval;
            _melee.animationSpeedMultiplier = _orig.animSpeed;
            _melee.maxRangeDelta = _orig.rangeDelta;
            _melee.recoilMinWeaponSpeed = _orig.recoilMin;
            _defense.minMaxDodgeCooldown = _orig.dodgeCooldown;
            RestoreDefensePreference();
            _melee.recoilNothingRiposteChance = _orig.riposteChance;
            _melee.minMaxRiposteDelay = _orig.riposteDelay;
            _defense.parryAdjustSpeed = _orig.parryAdjust;
            _defense.aboutToBeHitTiming = _orig.aboutToBeHit;
            _defense.dodgeChanceCurve = _orig.dodgeCurve;
            _defense.parryEnabled = _orig.parryEnabled;
            if (_ikEnabledByMe)
            {
                try
                {
                    Item right = creature.equipment.GetHeldItem(Side.Right);
                    _defense.DisableIK(right, Side.Right);
                }
                catch (Exception ex)
                {
                    Log.Debug_(_tag + " DisableIK failed: " + ex.Message);
                }
                _ikEnabledByMe = false;
            }
            _defense.oneHandUsesIK = _orig.oneHandIK;
            _defense.oneHandMaxIKWeightPos = _orig.oneHandPos;
            _defense.oneHandMaxIKWeightRot = _orig.oneHandRot;
            _defense.sideDodgeEnabled = _orig.sideDodge;
            _tuned = false;
            Log.Debug_(_tag + " tuning restored");
        }

        private static AnimationCurve ScaleCurve(AnimationCurve src, float mul)
        {
            Keyframe[] keys = src.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe k = keys[i];
                keys[i] = new Keyframe(k.time, Mathf.Clamp01(k.value * mul), k.inTangent * mul, k.outTangent * mul);
            }
            return new AnimationCurve(keys);
        }

        /// <summary>
        /// Mirrors what the stock HeldItemsChange does for a lone one-handed weapon, but only once the stock module has
        /// created its IK anchors (they appear the first time the NPC starts defending).
        /// </summary>
        private void TryEnableIk()
        {
            if (_defense.oneHandTransform == null || _defense.oneHandAnchor == null) return;
            Item right = creature.equipment.GetHeldItem(Side.Right);
            Item left = creature.equipment.GetHeldItem(Side.Left);
            if (right == null || right.data == null || right.data.moduleAI == null) return;
            if (right.data.moduleAI.primaryClass != ItemModuleAI.WeaponClass.Melee) return;
            if (left != null && left.data != null && left.data.moduleAI != null &&
                (left.data.moduleAI.primaryClass == ItemModuleAI.WeaponClass.Melee ||
                 left.data.moduleAI.primaryClass == ItemModuleAI.WeaponClass.Shield)) return;
            try
            {
                _defense.oneHandMaxIKWeightPos = _profile.OneHandIKPos;
                _defense.oneHandMaxIKWeightRot = _profile.OneHandIKRot;
                _defense.oneHandUsesIK = true; // anchors exist now, so the stock HeldItemsChange path is safe
                _defense.EnableIK(right, Side.Right);
                _ikEnabledByMe = true;
                Log.Debug_(_tag + " one-hand parry IK enabled for " + right.data.id);
            }
            catch (Exception ex)
            {
                // Experimental feature: if anything is off, give up on it for this creature only, for good.
                _ikFailed = true;
                _ikEnabledByMe = false;
                _defense.oneHandUsesIK = _orig.oneHandIK;
                _defense.oneHandMaxIKWeightPos = _orig.oneHandPos;
                _defense.oneHandMaxIKWeightRot = _orig.oneHandRot;
                Log.Warn(_tag + " could not enable weapon-placement IK, disabled for this creature: " + ex.Message);
            }
        }
    }
}
