using System;
using System.Reflection;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Game.Client.Entity
{
    // MossKnight has all the fields/properties/methods of HealthManagedEntity
    public class MossKnight : HealthManagedEntity
    {
        // Private fields created for MossKnight
        private Animation _lastAnimation;
        private readonly PlayMakerFSM _fsm;
        private readonly GameObject _gameObject;

        // Passes parameters to and calls constructor of HealthManagedEntity
        public MossKnight(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.MossKnight, entityId, gameObject)
        {
            // Initializes variables for MossKnight instance
            // Variables defined by Unity gameObject values
            _gameObject = gameObject;
            _fsm = gameObject.LocateMyFSM("Moss Knight Control");

            CreateAnimationEvents();
        }

        // Host: Whenever each animation happens in the fsm, it will send corresponding animation update
        private void CreateAnimationEvents()
        {
            _fsm.InsertMethod("Sleep", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Sleep); }));
            _fsm.InsertMethod("Shake", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Shake); }));
            _fsm.InsertMethod("Wake", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Wake); }));
            _fsm.InsertMethod("Lake", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Lake); }));
            _fsm.InsertMethod("Shield Left High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShieldLH); }));
            _fsm.InsertMethod("Shield Right High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShieldRH); }));
            _fsm.InsertMethod("Shield Left Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShieldLL); }));
            _fsm.InsertMethod("Shield Right Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShieldRL); }));
            _fsm.InsertMethod("Block High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.BlockH); }));
            _fsm.InsertMethod("Block Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.BlockL); }));
            _fsm.InsertMethod("Attack 1 Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Antic); }));
            _fsm.InsertMethod("Attack1 Lunge", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Lunge); }));
            _fsm.InsertMethod("Attack1 Hitbox On", 2, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.HitOn); }));
            _fsm.InsertMethod("Attack1 Hitbox Off", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.HitOff); }));
            _fsm.InsertMethod("Attack 1 End", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.End); }));
            _fsm.InsertMethod("Slash End", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.SlashEnd); }));
            _fsm.InsertMethod("Attack 2 Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Antic2); }));
            _fsm.InsertMethod("Attack 2 Slash", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Slash2); }));
            _fsm.InsertMethod("Attack 2 End", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.End2); }));
            _fsm.InsertMethod("Evade Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.EvadeAntic); }));
            _fsm.InsertMethod("Evade", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Evade); }));
            _fsm.InsertMethod("Evade End", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.EvadeEnd); }));
            _fsm.InsertMethod("Shoot Check", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShootCheck); }));
            _fsm.InsertMethod("Shoot Antic", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShootAntic); }));
            _fsm.InsertMethod("Shoot", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.Shoot); }));
            _fsm.InsertMethod("Shot End", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.ShotEnd); }));
            _fsm.InsertMethod("Unshield High", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.UnshieldH); }));
            _fsm.InsertMethod("Unshield Low", 0, CreateUpdateMethod(() => { SendAnimationUpdate((byte)Animation.UnshieldL); }));
        }

        protected override void InternalInitializeAsSceneHost()
        {
            // Updates MossKnight state as active, and then enables our walker component
            SendStateUpdate((byte)State.Active);
        }
        protected override void InternalInitializeAsSceneClient(byte? stateIndex)
        {
            RemoveAllTransitions(_fsm);
            if (stateIndex == (byte)State.Dead)
            {
                _gameObject.SetActive(false);
                Logger.Get().Info(this, "MossKnight Destroyed");
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
            Logger.Get().Info(this, "MossKnight: Switching to Scene Host");
            RestoreAllTransitions(_fsm);

            // Put our FSM back into the correct state based on the last state. 
            switch (_lastAnimation)
            {
                case Animation.Sleep:
                    _fsm.SetState("Sleep");
                    break;
                case Animation.Shake:
                    _fsm.SetState("Wake");
                    break;
                case Animation.Wake:
                    _fsm.SetState("Reset");
                    break;
                case Animation.Lake:
                    _fsm.SetState("Lake");
                    break;
                case Animation.ShieldLH:
                    _fsm.SetState("Shield Left High");
                    break;
                case Animation.ShieldRH:
                    _fsm.SetState("Shield Right High");
                    break;
                case Animation.ShieldLL:
                    _fsm.SetState("Shield Left Low");
                    break;
                case Animation.ShieldRL:
                    _fsm.SetState("Shield Right Low");
                    break;
                case Animation.BlockH:
                case Animation.BlockL:
                    _fsm.SetState("Attack Choice");
                    break;
                case Animation.Antic:
                    _fsm.SetState("Attack1 Lunge");
                    break;
                case Animation.Lunge:
                    _fsm.SetState("Attack1 Hitbox On");
                    break;
                case Animation.HitOn:
                    _fsm.SetState("Attack1 Hitbox Off");
                    break;
                case Animation.HitOff:
                    _fsm.SetState("Attack1 End");
                    break;
                case Animation.End:
                    _fsm.SetState("Slash 2?");
                    break;
                case Animation.SlashEnd:
                    _fsm.SetState("Reset");
                    break;
                case Animation.Antic2:
                    _fsm.SetState("Attack 2 Slash");
                    break;
                case Animation.Slash2:
                    _fsm.SetState("Attack 2 End");
                    break;
                case Animation.End2:
                    _fsm.SetState("Reset");
                    break;
                case Animation.EvadeAntic:
                    _fsm.SetState("Evade");
                    break;
                case Animation.Evade:
                    _fsm.SetState("Evade");
                    break;
                case Animation.EvadeEnd:
                    _fsm.SetState("Evade Move Check");
                    break;
                case Animation.ShootCheck:
                    _fsm.SetState("Shoot Check");
                    break;
                case Animation.ShootAntic:
                    _fsm.SetState("Shoot");
                    break;
                case Animation.Shoot:
                    _fsm.SetState("Repeat Check");
                    break;
                case Animation.ShotEnd:
                    _fsm.SetState("Reset");
                    break;
                case Animation.UnshieldH:
                case Animation.UnshieldL:
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
            if (animation == Animation.Sleep)
            {
                _fsm.ExecuteActions("Sleep", 6, 7, 8, 9, 10, 11, 12, 13);
            }

            if (animation == Animation.Shake)
            {
                _fsm.ExecuteActions("Shake", 1, 2, 4, 5);
            }

            if (animation == Animation.Wake)
            {
                _fsm.ExecuteActions("Wake", 1, 2, 3, 5, 7, 8);
            }

            if (animation == Animation.Lake)
            {
                _fsm.ExecuteActions("Lake", 2);
            }

            if (animation == Animation.ShieldLH)
            {
                _fsm.ExecuteActions("Shield Left High", 2, 5, 6);
            }

            if (animation == Animation.ShieldRH)
            {
                _fsm.ExecuteActions("Shield Right High", 2, 5, 6);
            }

            if (animation == Animation.ShieldLL)
            {
                _fsm.ExecuteActions("Shield Left Low", 2, 5, 6);
            }

            if (animation == Animation.ShieldRL)
            {
                _fsm.ExecuteActions("Shield Right Low", 2, 5, 6);
            }

            if (animation == Animation.BlockH)
            {
                _fsm.ExecuteActions("Block High", 1);
            }

            if (animation == Animation.BlockL)
            {
                _fsm.ExecuteActions("Block Low", 1);
            }

            if (animation == Animation.Antic)
            {
                _fsm.ExecuteActions("Attack 1 Antic", 1, 2, 3, 4, 5, 6, 7);
            }

            if (animation == Animation.Lunge)
            {
                _fsm.ExecuteActions("Attack1 Lunge", 1, 2, 3, 4, 7, 8);
            }

            if (animation == Animation.HitOn)
            {
                _fsm.ExecuteActions("Attack1 Hitbox On", 1);
            }

            if (animation == Animation.HitOff)
            {
                _fsm.ExecuteActions("Attack1 Hitbox Off", 1);
            }

            if (animation == Animation.End)
            {
                _fsm.ExecuteActions("Attack 1 End", 1, 2);
            }

            if (animation == Animation.SlashEnd)
            {
                _fsm.ExecuteActions("Slash End", 1);
            }

            if (animation == Animation.Antic2)
            {
                _fsm.ExecuteActions("Attack 2 Antic", 1, 2, 3, 4, 5, 6);
            }

            if (animation == Animation.Slash2)
            {
                _fsm.ExecuteActions("Attack 2 Slash", 1, 2, 5);
            }

            if (animation == Animation.End2)
            {
                _fsm.ExecuteActions("Attack 2 End", 2);
            }

            if (animation == Animation.EvadeAntic)
            {
                _fsm.ExecuteActions("Evade Antic", 2, 3);
            }

            if (animation == Animation.Evade)
            {
                _fsm.ExecuteActions("Evade", 1, 2);
            }

            if (animation == Animation.EvadeEnd)
            {
                _fsm.ExecuteActions("Evade End", 1, 2, 5);
            }

            if (animation == Animation.ShootCheck)
            {
                _fsm.ExecuteActions("Shoot Check", 1, 3);
            }

            if (animation == Animation.ShootAntic)
            {
                _fsm.ExecuteActions("Shoot Antic", 2);
            }

            if (animation == Animation.Shoot)
            {
                _fsm.ExecuteActions("Shoot", 1, 2, 3);
            }

            if (animation == Animation.ShotEnd)
            {
                _fsm.ExecuteActions("Shot End", 1);
            }

            if (animation == Animation.UnshieldH)
            {
                _fsm.ExecuteActions("Unshield High", 1);
            }

            if (animation == Animation.UnshieldL)
            {
                _fsm.ExecuteActions("Unshield Low", 1);
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
            Sleep = 0,
            Shake,
            Wake,
            Lake,
            ShieldLH,
            ShieldRH,
            ShieldLL,
            ShieldRL,
            BlockH,
            BlockL,
            Antic,
            Lunge,
            HitOn,
            HitOff,
            End,
            SlashEnd,
            Antic2,
            Slash2,
            End2,
            EvadeAntic,
            Evade,
            EvadeEnd,
            ShootCheck,
            ShootAntic,
            Shoot,
            ShotEnd,
            UnshieldH,
            UnshieldL
        }
    }
}