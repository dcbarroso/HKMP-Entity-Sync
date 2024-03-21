using System;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // HuskGuard has all the fields/properties/methods of HealthManagedEntity
    public class HuskGuard : HealthManagedEntity
    {
        // Private fields created for HuskGuard
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private readonly GameObject _gameObject;

        private readonly tk2dSpriteAnimator _animator;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public HuskGuard(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.HuskGuard, entityId, gameObject)
        {
            // Initializes variables for HuskGuard instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Zombie Guard");

            CreateAnimationEvents();
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Initiate", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Dormant); }));
            _fsm.InsertMethod("Wake", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Wake); }));
            _fsm.InsertMethod("Cooldown", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Anticipate", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Anticipate); }));
            _fsm.InsertMethod("Attack", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Attack2); }));
            _fsm.InsertMethod("Impact", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Impact); }));
            _fsm.InsertMethod("Impact Left", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ImpactLeft); }));
            _fsm.InsertMethod("Impact Right", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ImpactRight); }));
            _fsm.InsertMethod("Attack Recoil", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.AttackRecoil); }));
            _fsm.InsertMethod("Stomp Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.StompAntic); }));
            _fsm.InsertMethod("Stomp Jump", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.StompJump); }));
            _fsm.InsertMethod("Land", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.StompLand); }));
            _fsm.InsertMethod("Stomp Cooldown", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
            _fsm.InsertMethod("Startle", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Startle); }));
            _fsm.InsertMethod("Run", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Run); }));
            _fsm.InsertMethod("Stop Run", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.StopRun); }));
            _fsm.InsertMethod("Walk", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Walk); }));
            _fsm.InsertMethod("Stop Walk", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.StopWalk); }));
            _fsm.InsertMethod("Turn Right", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Turn); }));
            _fsm.InsertMethod("Turn Right 2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Turn); }));
            _fsm.InsertMethod("Return Right", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Walk); }));
            _fsm.InsertMethod("Turn Left", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Turn); }));
            _fsm.InsertMethod("Turn Left 2", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Turn); }));
            _fsm.InsertMethod("Return Left", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Walk); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Husk state as active, and then enables our walker component
            Logger.Get().Debug(this, "Host: Internally Initializing HuskGuard");
            SendStateUpdate((byte)State.Active);
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // Removes our FSM transitions
            RemoveAllTransitions(_fsm);
            Logger.Get().Debug(this, "Client: Internally Initializing HuskGuard");
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "Entity Destroyed");
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
            Logger.Get().Info(this, "HuskGuard: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Dormant:
                    _fsm.SetState("Initiate");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Wake:
                    _fsm.SetState("Cooldown");
                    break;
                case Animation.Anticipate:
                    _fsm.SetState("Attack");
                    break;
                case Animation.Attack2:
                    _fsm.SetState("Impact");
                    break;
                case Animation.StompAntic:
                    _fsm.SetState("Stomp Jump");
                    break;
                case Animation.StompJump:
                    _fsm.SetState("In Air");
                    break;
                case Animation.StompLand:
                    _fsm.SetState("Stomp Cooldown");
                    break;
                case Animation.Startle:
                    _fsm.SetState("Alert");
                    break;
                case Animation.Run:
                    _fsm.SetState("Face Hero");
                    break;
                case Animation.StopRun:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Walk:
                    _fsm.SetState("Face Hero");
                    break;
                case Animation.StopWalk:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Impact:
                    _fsm.SetState("Attack Recoil");
                    break;
                case Animation.ImpactLeft:
                    _fsm.SetState("Attack Recoil");
                    break;
                case Animation.ImpactRight:
                    _fsm.SetState("Attack Recoil");
                    break;
                case Animation.AttackRecoil:
                    _fsm.SetState("Attack End");
                    break;
            }
        }

        // Impact and Recoil states were handled through animation updates despite having no animation, couldnt get states to update.
        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo)
        {
            // Updates Animation according to HealthManagerEntity instructions
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation)animationIndex;

            _lastAnimation = animation;

            // Client: Executes Animations
            if (animation == Animation.Dormant)
            {
                _fsm.ExecuteActions("Initiate", 7);
            }

            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Idle", 5);
            }

            if (animation == Animation.Wake)
            {
                _fsm.ExecuteActions("Wake", 1, 2, 3, 5);
            }

            if (animation == Animation.Anticipate)
            {
                _fsm.ExecuteActions("Anticipate", 1, 3, 5, 6);
            }

            if (animation == Animation.Attack2)
            {
                _fsm.ExecuteActions("Attack", 1, 2);
            }

            if (animation == Animation.Impact)
            {
                _fsm.ExecuteActions("Impact", 1, 2);
            }

            if (animation == Animation.ImpactLeft)
            {
                _fsm.ExecuteActions("Impact Left", 1);
            }
            if (animation == Animation.ImpactRight)
            {
                _fsm.ExecuteActions("Impact Right", 1);
            }

            if (animation == Animation.AttackRecoil)
            {
                _fsm.ExecuteActions("Attack Recoil", 1, 2, 3);
            }

            if (animation == Animation.StompAntic)
            {
                _fsm.ExecuteActions("Stomp Antic", 1, 2);
            }

            if (animation == Animation.StompJump)
            {
                _fsm.ExecuteActions("Stomp Jump", 1, 2);
            }

            if (animation == Animation.StompLand)
            {
                _fsm.ExecuteActions("Land", 1, 2, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            }

            if (animation == Animation.Startle)
            {
                _fsm.ExecuteActions("Startle", 1, 3);
            }

            if (animation == Animation.Run)
            {
                _fsm.ExecuteActions("Run", 1, 2, 3, 4, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17);
            }

            if (animation == Animation.StopRun)
            {
                _fsm.ExecuteActions("Stop Run", 1, 2, 3, 4, 5, 9, 10);
            }

            if (animation == Animation.Walk)
            {
                _fsm.ExecuteActions("Walk", 1, 2, 4, 5, 7);
            }

            if (animation == Animation.StopWalk)
            {
                _fsm.ExecuteActions("Stop Walk", 1, 4, 5);
            }
        }

        public override void UpdateState(byte stateIndex)
        {
        }

        private enum State
        {
            Active = 0,
            Dead,
        }

        private enum Animation
        {
            Dormant= 0,
            Wake,
            Idle,
            Walk,
            Run,
            StopWalk,
            StopRun,
            Turn,
            Attack2,
            StompAntic,
            StompJump,
            StompLand,
            Startle,
            Anticipate,
            Impact,
            ImpactLeft,
            ImpactRight,
            AttackRecoil
        }
    }
}