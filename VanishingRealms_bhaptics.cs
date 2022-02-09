using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MelonLoader;
using HarmonyLib;
using UnityEngine;

using MyBhapticsTactsuit;

namespace VanishingRealms_bhaptics
{
    public class VanishingRealms_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;
        public static bool bladeRightHand = true;
        public static bool shieldRightHand = false;
        public static bool bowRightHand = false;
        public static bool wandRightHand = true;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        
        [HarmonyPatch(typeof(Weapon), "DoBladeHit", new Type[] { typeof(RaycastHit), typeof(Vector3) })]
        public class bhaptics_BladeHit
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon __instance)
            {
                //tactsuitVr.LOG("BladeHit: " + __instance.toolID.ToString() + " " + __instance.isTwoHanded.ToString());
                tactsuitVr.Recoil("Blade", bladeRightHand);
            }
        }

        [HarmonyPatch(typeof(Weapon), "DoHandleHit", new Type[] { typeof(RaycastHit), typeof(Vector3) })]
        public class bhaptics_HandleHit
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon __instance)
            {
                //tactsuitVr.LOG("HandleHit: " + __instance.toolID.ToString() + " " + __instance.isTwoHanded.ToString());
                tactsuitVr.Recoil("Blade", bladeRightHand);
            }
        }

        [HarmonyPatch(typeof(WeaponBow), "Shoot", new Type[] { })]
        public class bhaptics_BowShoot
        {
            [HarmonyPostfix]
            public static void Postfix(WeaponBow __instance)
            {
                //tactsuitVr.LOG("BowShoot: ");
                tactsuitVr.Recoil("Bow", !bowRightHand);
            }
        }

        [HarmonyPatch(typeof(MagicSpell), "DeploySpell", new Type[] { typeof(Vector3), typeof(Vector3) })]
        public class bhaptics_ShootSpell
        {
            [HarmonyPostfix]
            public static void Postfix(MagicSpell __instance)
            {
                tactsuitVr.Spell("Fire", wandRightHand);
            }
        }


        [HarmonyPatch(typeof(PlayerBody), "OnDie", new Type[] { })]
        public class bhaptics_PlayerDie
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        private static KeyValuePair<float, float> getAngleAndShift(Transform player, Vector3 hit)
        {
            // bhaptics pattern starts in the front, then rotates to the left. 0° is front, 90° is left, 270° is right.
            // y is "up", z is "forward" in local coordinates
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            Vector3 hitPosition = hit - player.position;
            Quaternion myPlayerRotation = player.rotation;
            Vector3 playerDir = myPlayerRotation.eulerAngles;
            // get rid of the up/down component to analyze xz-rotation
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);

            // get angle. .Net < 4.0 does not have a "SignedAngle" function...
            float hitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            // check if cross product points up or down, to make signed angle myself
            Vector3 crossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (crossProduct.y > 0f) { hitAngle *= -1f; }
            // relative to player direction
            float myRotation = hitAngle - playerDir.y;
            // switch directions (bhaptics angles are in mathematically negative direction)
            myRotation *= -1f;
            // convert signed angle into [0, 360] rotation
            if (myRotation < 0f) { myRotation = 360f + myRotation; }


            // up/down shift is in y-direction
            // in Shadow Legend, the torso Transform has y=0 at the neck,
            // and the torso ends at roughly -0.5 (that's in meters)
            // so cap the shift to [-0.5, 0]...
            float hitShift = hitPosition.y;
            float upperBound = 0.0f;
            float lowerBound = -0.5f;
            if (hitShift > upperBound) { hitShift = 0.5f; }
            else if (hitShift < lowerBound) { hitShift = -0.5f; }
            // ...and then spread/shift it to [-0.5, 0.5]
            else { hitShift = (hitShift - lowerBound) / (upperBound - lowerBound) - 0.5f; }

            //tactsuitVr.LOG("Relative x-z-position: " + relativeHitDir.x.ToString() + " "  + relativeHitDir.z.ToString());
            //tactsuitVr.LOG("HitAngle: " + hitAngle.ToString());
            //tactsuitVr.LOG("HitShift: " + hitShift.ToString());

            // No tuple returns available in .NET < 4.0, so this is the easiest quickfix
            return new KeyValuePair<float, float>(myRotation, hitShift);
        }


        [HarmonyPatch(typeof(PlayerBody), "OnDamage", new Type[] { typeof(DamageInfo) })]
        public class bhaptics_PlayerDamage
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerBody __instance, DamageInfo damageInfo)
            {
                if (damageInfo.hitLocationName == HitLocationName.Weapon) { tactsuitVr.Recoil("Blade", bladeRightHand, true, 0.5f); return; }
                if (damageInfo.hitLocationName == HitLocationName.Shield) { tactsuitVr.Recoil("Blade", shieldRightHand, true, 0.5f); return; }
                if (damageInfo.hitLocationName == HitLocationName.ArmLeft) { tactsuitVr.PlaybackHaptics("Recoil_L"); return; }
                if (damageInfo.hitLocationName == HitLocationName.ArmRight) { tactsuitVr.PlaybackHaptics("Recoil_R"); return; }
                if (damageInfo.hitLocationName == HitLocationName.HandLeft) { tactsuitVr.PlaybackHaptics("RecoilHands_L"); return; }
                if (damageInfo.hitLocationName == HitLocationName.HandRight) { tactsuitVr.PlaybackHaptics("RecoilHands_R"); return; }
                if (damageInfo.damage == 0f) return;

                //tactsuitVr.LOG("Damage: " + damageInfo.hitLocationName.ToString() + " " + damageInfo.hitLocation.x.ToString() + " " + __instance.transform.position.x.ToString());
                var angleShift = getAngleAndShift(__instance.transform, damageInfo.hitLocation);
                if (damageInfo.hitLocationName == HitLocationName.Head) { tactsuitVr.HeadShot("Impact", angleShift.Key); return; }
                tactsuitVr.PlayBackHit("Impact", angleShift.Key, angleShift.Value);
            }
        }

        [HarmonyPatch(typeof(ClimbAndFall), "Fall", new Type[] { })]
        public class bhaptics_PlayerFall
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("FallDamage");
                tactsuitVr.PlaybackHaptics("FallDamageFeet");
            }
        }

        [HarmonyPatch(typeof(EvtGame), "PlayerTakeHealth", new Type[] { typeof(float) })]
        public class bhaptics_PlayerHeal
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Healing");
            }
        }

        [HarmonyPatch(typeof(EvtGame), "PlayerTakeFireDamage", new Type[] { typeof(float) })]
        public class bhaptics_PlayerBurning
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Burning");
            }
        }



        [HarmonyPatch(typeof(EvtGame), "PlayerAddHealthBonus", new Type[] { typeof(int) })]
        public class bhaptics_PlayerHealthBonus
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Healing");
            }
        }

        [HarmonyPatch(typeof(ItemPotion), "StartDrinking", new Type[] {  })]
        public class bhaptics_PlayerDrink
        {
            [HarmonyPostfix]
            public static void Postfix(ItemPotion __instance)
            {
                tactsuitVr.PlaybackHaptics("Drinking");
                if (__instance.potionType == PotionType.health) tactsuitVr.PlaybackHaptics("Healing");
                if (__instance.potionType == PotionType.poison) tactsuitVr.PlaybackHaptics("Poison");
            }
        }

        [HarmonyPatch(typeof(TheHapticsManager), "Vibrate", new Type[] { typeof(int), typeof(float), typeof(float) })]
        public class bhaptics_Vibrate
        {
            [HarmonyPostfix]
            public static void Postfix(int controllerIndex)
            {
                tactsuitVr.LOG("Vibrate int: " + controllerIndex.ToString());
            }
        }

        [HarmonyPatch(typeof(Grabber), "Grab", new Type[] { typeof(GameObject), typeof(bool), typeof(bool) })]
        public class bhaptics_PlayerGrab
        {
            [HarmonyPostfix]
            public static void Postfix(Grabber __instance, GameObject item)
            {
                if (item.name.Contains("Sword")) { bladeRightHand = (__instance.controllerIndex == 2); return; }
                if (item.name.Contains("Shield")) { shieldRightHand = (__instance.controllerIndex == 2); return; }
                if (item.name.Contains("Bow")) { bowRightHand = (__instance.controllerIndex == 2); return; }
                //if (item.name.Contains("Wand")) { wandRightHand = (__instance.controllerIndex == 2); return; }
                //tactsuitVr.LOG("Grabbed: " + item.name + " " + __instance.controllerIndex.ToString());
            }
        }

    }
}
