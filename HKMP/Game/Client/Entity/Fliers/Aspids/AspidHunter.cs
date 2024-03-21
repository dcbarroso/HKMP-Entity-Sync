using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // AspidHunter has all the fields/properties/methods of HealthManagedEntity
    public class AspidHunter : HealthManagedEntity
    {
        // Private fields created for AspidHunter
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;
        private readonly tk2dSpriteAnimator _animator;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public AspidHunter(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.AspidHunter, entityId, gameObject)
        {
            // Initializes variables for AspidHunter instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("spitter");
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
                    //Logger.Get().Info(this, $"Listing AspidHunter Animations: {(string) clip.name}");
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
                /*if (currentClip.name == "Fire")
                {
                    SendAnimationUpdate((byte)Animation.Fire);
                    Logger.Get().Info(this, $"AspidHunter {entityId} should be firing now");
                }
                if (currentClip.name == "TurnToFly")
                {
                    SendAnimationUpdate((byte)Animation.TurnToFly);
                    Logger.Get().Info(this, $"AspidHunter {entityId} should be turning now");
                }*/
            };
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Distance Fly", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.DFly); }));
            _fsm.InsertMethod("Distance Fly 2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.DFly2); }));
            _fsm.InsertMethod("Fire Anticipate", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FireAnticipate); }));
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Alert", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Alert); }));
            _fsm.InsertMethod("Fly Back", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FlyBack); }));
            _fsm.InsertMethod("Fire", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Fire); }));
            _fsm.InsertMethod("Fire Dribble", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.FireDribble); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates AspidHunter state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            _animator.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            _animator.enabled = false;
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "AspidHunter Destroyed");
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
            Logger.Get().Info(this, "AspidHunter: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            //_climber.enabled = true;
            _animator.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.DFly:
                    _fsm.SetState("Raycast");
                    break;
                case Animation.DFly2:
                    _fsm.SetState("Distance Fly 2");
                    break;
                case Animation.FireAnticipate:
                    _fsm.SetState("Fire");
                    break;
                case Animation.Fire:
                    _fsm.SetState("FireDribble");
                    break;
                case Animation.FireDribble:
                    _fsm.SetState("Distance Fly");
                    break;
                case Animation.FlyBack:
                    _fsm.SetState("Fire Anticipate");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Alert:
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
                _fsm.ExecuteActions("Distance Fly", 1, 2, 4);
                Logger.Get().Info(this, "Aspid Executing DFly");
            }

            if (animation == Animation.DFly2)
            {
                _fsm.ExecuteActions("Distance Fly 2", 1, 2, 5);
                Logger.Get().Info(this, "Aspid Executing DFly2");
            }

            if (animation == Animation.FireAnticipate)
            {
                _fsm.ExecuteActions("Fire Anticipate", 2);
                Logger.Get().Info(this, "Aspid Executing Anticipate");
            }

            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Idle", 1, 2, 3, 4);
                Logger.Get().Info(this, "Aspid Executing Idle");
            }

            if (animation == Animation.Alert)
            {
                _fsm.ExecuteActions("Alert", 1);
                Logger.Get().Info(this, "Aspid Executing Alert");
            }

            if (animation == Animation.FlyBack)
            {
                _fsm.ExecuteActions("Fly Back", 3);
                Logger.Get().Info(this, "Aspid Executing FlyBack");
            }

            if (animation == Animation.Fire)
            {
                _fsm.ExecuteActions("Fire", 1, 2, 3);
                Logger.Get().Info(this, "Aspid Executing Fire");
            }

            if (animation == Animation.FireDribble)
            {
                _fsm.ExecuteActions("Fire Dribble", 6);
                Logger.Get().Info(this, "Aspid Executing Dribble");
            }

                // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
            /*    if (animation == Animation.Fire)
            {
                _animator.Play("Fire");
                Logger.Get().Info(this, "AspidHunter Playing Fire");
            }
            if (animation == Animation.TurnToFly)
            {
                _animator.Play("TurnToFly");
                Logger.Get().Info(this, "AspidHunter Playing TurnToFly");
            }*/
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
            Fly = 0,
            Fire,
            FireLong,
            Idle,
            TurnToFly,
            DFly,
            DFly2,
            FireAnticipate,
            Alert,
            FlyBack,
            FireDribble
        }
    }
}