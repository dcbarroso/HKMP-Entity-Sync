using System;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // HuskWarrior has all the fields/properties/methods of HealthManagedEntity
    public class HuskWarrior : HealthManagedEntity
    {
        // Private fields created for HuskWarrior
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private readonly Walker _walker;

        private readonly AudioSource _audioSource;

        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public HuskWarrior(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.HuskWarrior, entityId, gameObject)
        {
            // Initializes variables for HuskWarrior instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("ZombieShieldControl");
            _walker = gameObject.GetComponent<Walker>();
            _audioSource = gameObject.GetComponent<AudioSource>();
            CreateAnimationEvents();
            var walkerAnimator = gameObject.GetComponent<Walker>().GetComponent<tk2dSpriteAnimator>();


            // Some animations are not controlled by the FSM. Hence we must make all animations in the entity's Walker component to trigger `AnimationEventTriggered` to send state updates. 
            if (walkerAnimator != null)
            {
                foreach (var clip in walkerAnimator.Library.clips)
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
                }
            }
            else
            {
                Logger.Get().Warn(this, "Walker animator not found");
            }
            // Making each animation send an update
            // Lambda expression receives the parameters of AnimationEventTriggered, which should be a method within tk2dspriteanimator component
            walkerAnimator.AnimationEventTriggered = (caller, currentClip, currentFrame) => {
                if (currentClip.name == "Idle")
                {
                    SendAnimationUpdate((byte)Animation.Idle);
                }
                else if (currentClip.name == "Walk")
                {
                    SendAnimationUpdate((byte)Animation.Walk);
                }
                else if (currentClip.name == "Turn")
                {
                    SendAnimationUpdate((byte)Animation.Turn);
                }
            };
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Shield Start", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Start); }));
            _fsm.InsertMethod("Shield Left High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LHigh); }));
            _fsm.InsertMethod("Shield Left Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LLow); }));
            _fsm.InsertMethod("Shield Right High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RHigh); }));
            _fsm.InsertMethod("Shield Right Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.RLow); }));
            _fsm.InsertMethod("Attack1 Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A1Antic); }));
            _fsm.InsertMethod("Attack1 End", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A1End); }));
            _fsm.InsertMethod("Attack1 Lunge", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A1Lunge); }));
            _fsm.InsertMethod("Attack1 Slash", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A1Slash); }));
            _fsm.InsertMethod("Block Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.BLow); }));
            _fsm.InsertMethod("Block High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.BHigh); }));
            _fsm.InsertMethod("Attack3 Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3Antic); }));
            _fsm.InsertMethod("A3 Lunge 1", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3Lunge1); }));
            _fsm.InsertMethod("A3 Slash 1", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3Slash1); }));
            _fsm.InsertMethod("A3 CD 1", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3CD1); }));
            _fsm.InsertMethod("A3 Lunge 2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3Lunge2); }));
            _fsm.InsertMethod("A3 CD2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3CD2); }));
            _fsm.InsertMethod("A3 Lunge 3", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3Lunge3); }));
            _fsm.InsertMethod("A3 Slash3", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3Slash3); }));
            _fsm.InsertMethod("A3 Stop1 3", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.A3Stop); }));
            _fsm.InsertMethod("Unshield Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ULow); }));
            _fsm.InsertMethod("Unshield High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.UHigh); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Husk state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
            _walker.enabled = true;
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // Removes our FSM transitions and disables walker component
            RemoveAllTransitions(_fsm);
            _walker.Stop(Walker.StopReasons.Bored); // Stops enemy from 'drifting' when entering the room. 
            _walker.enabled = false;
            Logger.Get().Info(this, "InternalInitializeAsSceneClient invoked");
            if (stateIndex == (byte)State.Dead)
            {
                // Disables entity game object if state is set to dead
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Entity Destroyed");
            }
        }

        // Invoked when entity dies for host
        protected override void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            // Call the base implementation to execute the original functionality
            base.HealthManagerOnDieHook(orig, self, attackDirection, attackType, ignoreEvasion);

            // Send Dead State
            SendStateUpdate((byte)State.Dead);
        }

        protected override void InternalSwitchToSceneHost()
        {
            Logger.Get().Info(this, "HuskWarrior: Switching to Scene Host");
            RestoreAllTransitions(_fsm);
            _walker.enabled = true;
            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Walk:
                    _fsm.SetState("Reset");
                    break;
                case Animation.Start:
                    _fsm.SetState("Start");
                    break;
                case Animation.LHigh:
                    _fsm.SetState("Shield Left High");
                    break;
                case Animation.LLow:
                    _fsm.SetState("Shield Left Low");
                    break;
                case Animation.RHigh:
                    _fsm.SetState("Shield Right High");
                    break;
                case Animation.RLow:
                    _fsm.SetState("Shield Right Low");
                    break;
                case Animation.A1Antic:
                    _fsm.SetState("Attack1 Lunge");
                    break;
                case Animation.A1Lunge:
                    _fsm.SetState("Attack1 Slash");
                    break;
                case Animation.A1Slash:
                    _fsm.SetState("Attack 1 End");
                    break;
                case Animation.A1End:
                    _fsm.SetState("Reset");
                    break;
                case Animation.BLow:
                    _fsm.SetState("Attack 3 Antic");
                    break;
                case Animation.BHigh:
                    _fsm.SetState("Attack 3 Antic");
                    break;
                case Animation.A3Antic:
                    _fsm.SetState("A3 Lunge 1");
                    break;
                case Animation.A3Lunge1:
                    _fsm.SetState("A3 Slash 1");
                    break;
                case Animation.A3Slash1:
                    _fsm.SetState("A3 CD 1");
                    break;
                case Animation.A3CD1:
                    _fsm.SetState("A3 Lunge 2");
                    break;
                case Animation.A3Lunge2:
                    _fsm.SetState("A3 CD2");
                    break;
                case Animation.A3CD2:
                    _fsm.SetState("A3 Lunge 3");
                    break;
                case Animation.A3Lunge3:
                    _fsm.SetState("A3 Slash3");
                    break;
                case Animation.A3Slash3:
                    _fsm.SetState("A3 Stop");
                    break;
                case Animation.A3Stop:
                case Animation.ULow:
                case Animation.UHigh:
                    _fsm.SetState("Reset");
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
            if (animation == Animation.Start)
            {
                _fsm.ExecuteActions("Shield Start", 1);
            }

            if (animation == Animation.LHigh)
            {
                _fsm.ExecuteActions("Shield Left High", 2, 4, 6, 7);
            }

            if (animation == Animation.LLow)
            {
                _fsm.ExecuteActions("Shield Left Low", 2, 4, 7, 8);
            }

            if (animation == Animation.RHigh)
            {
                _fsm.ExecuteActions("Shield Right High", 2, 4, 6, 7);
            }

            if (animation == Animation.RLow)
            {
                _fsm.ExecuteActions("Shield Right Low", 2, 4, 6, 7);
            }

            if (animation == Animation.A1Antic)
            {
                _fsm.ExecuteActions("Attack1 Antic", 1, 2);
            }

            if (animation == Animation.A1End)
            {
                _fsm.ExecuteActions("Attack1 End", 1, 2);
            }

            if (animation == Animation.A1Lunge)
            {
                _fsm.ExecuteActions("Attack1 Lunge", 1, 2, 4, 5);
            }

            if (animation == Animation.A1Slash)
            {
                _fsm.ExecuteActions("Attack1 Slash", 1, 3);
            }

            if (animation == Animation.BLow)
            {
                _fsm.ExecuteActions("Block Low", 1);
            }

            if (animation == Animation.BHigh)
            {
                _fsm.ExecuteActions("Block High", 1);
            }

            if (animation == Animation.A3Antic)
            {
                _fsm.ExecuteActions("Attack3 Antic", 1, 2);
            }

            if (animation == Animation.A3Lunge1)
            {
                _fsm.ExecuteActions("A3 Lunge 1", 1, 2, 4, 5);
            }

            if (animation == Animation.A3Slash1)
            {
                _fsm.ExecuteActions("A3 Slash 1", 1, 2);
            }

            if (animation == Animation.A3CD1)
            {
                _fsm.ExecuteActions("A3 CD 1", 1, 2);
            }

            if (animation == Animation.A3Lunge2)
            {
                _fsm.ExecuteActions("A3 Lunge 2", 1, 3, 4, 5);
            }

            if (animation == Animation.A3CD2)
            {
                _fsm.ExecuteActions("A3 CD2", 2, 3);
            }

            if (animation == Animation.A3Lunge3)
            {
                _fsm.ExecuteActions("A3 Lunge 3", 1, 3, 4);
            }

            if (animation == Animation.A3Slash3)
            {
                _fsm.ExecuteActions("A3 Slash3", 2, 3);
            }

            if (animation == Animation.A3Stop)
            {
                _fsm.ExecuteActions("A3 Stop1 3", 2, 3);
            }

            if (animation == Animation.ULow)
            {
                _fsm.ExecuteActions("Unshield Low", 1);
            }

            if (animation == Animation.UHigh)
            {
                _fsm.ExecuteActions("Unshield High", 1);
            }

            if (animation == Animation.Idle)
            {
                // This animation is not controlled by the FSM. It must be started manually from the entity's `Walker` `SpriteAnimator`
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                _audioSource.Stop();
            }

            if (animation == Animation.Walk)
            {
                _audioSource.Play();
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Walk");
            }

            if (animation == Animation.Turn)
            {
                _walker.GetComponent<tk2dSpriteAnimator>().Play("Turn");
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
            Start = 0,
            LHigh,
            LLow,
            RLow,
            RHigh,
            A1Antic,
            A1Lunge,
            A1Slash,
            A1End,
            BHigh,
            BLow,
            A3Antic,
            A3Lunge1,
            A3Slash1,
            A3CD1,
            A3Lunge2,
            A3CD2,
            A3Lunge3,
            A3Slash3,
            A3Stop,
            UHigh,
            ULow,
            Idle,
            Walk,
            Turn,
        }
    }
}