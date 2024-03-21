using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // AspidMother has all the fields/properties/methods of HealthManagedEntity
    public class AspidMother : HealthManagedEntity
    {
        // Private fields created for AspidMother
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;
        private readonly tk2dSpriteAnimator _animator;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public AspidMother(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.AspidMother, entityId, gameObject)
        {
            // Initializes variables for AspidMother instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Hatcher");
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
            CreateAnimationEvents();


            // Some animations are not controlled by the FSM. Hence we must make all animations in the entity's Walker component to trigger `AnimationEventTriggered` to send state updates. 
            if (_animator != null)
            {
                foreach (var clip in _animator.Library.clips)
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
                    //Logger.Get().Info(this, $"Listing AspidMother Animations: {(string) clip.name}");
                }
            }
            else
            {
                Logger.Get().Warn(this, "Animator not found");
            }
            // Making each animation send an update
            // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dspriteanimator component
            _animator.AnimationEventTriggered = (caller, currentClip, currentFrame) =>
            {
                if (currentClip.name == "Burst")
                {
                    SendAnimationUpdate((byte)Animation.Burst);
                    Logger.Get().Info(this, $"AspidMother {entityId} should be bursting now");
                }
            };
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Distance Fly", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.DFly); }));
            _fsm.InsertMethod("Fire Anticipate", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Anticipate); }));
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Fire", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Fire); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates AspidMother state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            _animator.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            _animator.enabled = false;
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                //UnityEngine.Object.Destroy(_gameObject);
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "AspidMother Destroyed");
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
            Logger.Get().Info(this, "AspidMother: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            _animator.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.DFly:
                    _fsm.SetState("Hatched Max Check");
                    break;
                case Animation.Fire:
                    _fsm.SetState("Distance Fly");
                    break;
                case Animation.Anticipate:
                    _fsm.SetState("Fire");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Distance Fly");
                    break;
            }

        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;


            // Client: Executes Animations
            if (animation == Animation.DFly)
            {
                _fsm.ExecuteActions("Distance Fly", 1, 2, 3);
            }

            if (animation == Animation.Fire)
            {
                _fsm.ExecuteActions("Fire", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
                Logger.Get().Info(this, "AspidMother Executing Fire");
            }
            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Idle", 1, 2);
            }

            if (animation == Animation.Anticipate)
            {
                _fsm.ExecuteActions("Fire Anticipate", 2);
                Logger.Get().Info(this, "AspidMother Executing Fire Anticipate");
            }

            // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
            if (animation == Animation.Burst)
            {
                _animator.Play("Burst");
                Logger.Get().Info(this, "AspidMother Playing Burst");
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
            DFly = 0,
            Fire,
            FireLong,
            Idle,
            TurnToFly,
            Burst,
            Anticipate
        }
    }
}