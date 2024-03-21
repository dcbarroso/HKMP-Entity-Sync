using System;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // Baldur has all the fields/properties/methods of HealthManagedEntity
    public class Baldur : HealthManagedEntity
    {
        // Private fields created for Baldur
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private readonly tk2dSpriteAnimator _animator;

        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public Baldur(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.Baldur, entityId, gameObject)
        {
            // Initializes variables for Baldur instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Roller");
            CreateAnimationEvents();
            _animator = gameObject.GetComponent<tk2dSpriteAnimator>();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Stop", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Stop); }));
            _fsm.InsertMethod("Start", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Start); }));
            _fsm.InsertMethod("Roll R", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Roll); }));
            _fsm.InsertMethod("Roll L", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Roll); }));
            _fsm.InsertMethod("Collide Right", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Collide); }));
            _fsm.InsertMethod("Collide Left", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Collide); }));
            _fsm.InsertMethod("Land", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Land); }));
            _fsm.InsertMethod("Left or right?", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.LoR); }));
            _fsm.InsertMethod("Rest", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Idle); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates Husk state as active, and then enables our walker component
            Logger.Get().Info(this, "Host: Internally Initializing Baldur");
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            // Removes our FSM transitions and disables walker component
            RemoveAllTransitions(_fsm);
            Logger.Get().Info(this, "Client: Internally Initializing Baldur");
            if (stateIndex == 1)
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
            Logger.Get().Info(this, "Baldur: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Start:
                    _fsm.SetState("Left or right?");
                    break;
                case Animation.LoR:
                    _fsm.SetState("Left or right?");
                    break;
                case Animation.Land:
                    _fsm.SetState("Left or right?");
                    break;
                case Animation.Roll:
                    _fsm.SetState("Stop");
                    break;
                case Animation.Stop:
                    _fsm.SetState("Rest");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Collide:
                    _fsm.SetState("In Air");
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
                _fsm.ExecuteActions("Start", 1, 3, 4);
            }

            if (animation == Animation.LoR)
            {
                _fsm.ExecuteActions("Left or Right?", 1, 2, 3);
            }

            if (animation == Animation.Idle)
            {
                _fsm.ExecuteActions("Rest", 1);
            }

            if (animation == Animation.Land)
            {
                _fsm.ExecuteActions("Start", 1, 2);
            }

            if (animation == Animation.Collide)
            {
                _fsm.ExecuteActions("Collide Right", 1, 2, 7);
            }

            if (animation == Animation.Roll)
            {
                _fsm.ExecuteActions("Roll R", 2);
            }

            if (animation == Animation.Stop)
            {
                _fsm.ExecuteActions("Stop", 1, 2, 4, 5);
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
            Idle = 0,
            Stop,
            Start,
            Roll,
            Collide,
            LoR,
            Land
        }
    }
}