using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Tiktik has all the fields/properties/methods of HealthManagedEntity
    public class Tiktik : HealthManagedEntity
    {
        // Private fields created for Tiktik
        private Animation _lastAnimation;
        private readonly Climber _climber;
        private readonly tk2dSpriteAnimator climberAnimator;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Tiktik(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Tiktik, entityId, gameObject)
        {
            // Initializes variables for Tiktik instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _climber = gameObject.GetComponent<Climber>();
            climberAnimator = gameObject.GetComponent<Climber>().GetComponent<tk2dSpriteAnimator>();

            // Some animations are not controlled by the FSM. Hence we must make all animations in the entity's Walker component to trigger `AnimationEventTriggered` to send state updates. 
            if (climberAnimator != null)
            {
                foreach (var clip in climberAnimator.Library.clips)
                {
                    // Skip clips with no frames
                    if (clip.frames.Length == 0)
                    {
                        continue;
                    }
                    var firstFrame = clip.frames[0];
                    // Enable event triggering on the first frame
                    // Whenever frame displayed has triggerEvent set to true, AnimationEventTriggered is called
                    firstFrame.triggerEvent = true;
                    // Also include the clip name as event info
                    firstFrame.eventInfo = clip.name;
                    //Logger.Get().Info(this, $"Listing Tiktik Animations: {(string) clip.name}");
                }
            }
            else
            {
                Logger.Get().Warn(this, "Climber animator not found");
            }
            // Making each animation send an update
            // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dspriteanimator component
            climberAnimator.AnimationEventTriggered = (caller, currentClip, currentFrame) =>
            {
                if (currentClip.name == "Walk")
                {
                    SendAnimationUpdate((byte)Animation.Walk);
                }
            };
        }


        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Tiktik state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            _climber.enabled = true;
            climberAnimator.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // disables climber component
            _climber.enabled = false;
            climberAnimator.enabled = false;
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Tiktik Destroyed");
            }
        }

        protected override void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            // Call the base implementation to execute the original functionality
            base.HealthManagerOnDieHook(orig, self, attackDirection, attackType, ignoreEvasion);

            // Send Dead State
            SendStateUpdate((byte)State.Dead);
        }

        protected override void InternalSwitchToSceneHost()
        {
            Logger.Get().Info(this, "Tiktik: Switching to Scene Host");
            _climber.enabled = true;
            climberAnimator.enabled = true;
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;

            // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
            if (animation == Animation.Walk)
            {
                _climber.GetComponent<tk2dSpriteAnimator>().Play("walk");
            }
        }

        public override void UpdateState(byte stateIndex)
        {
        }

        private enum State
        {
            Active = 0,
            Dead
        }

        private enum Animation
        {
            Walk = 0,
        }
    }
}