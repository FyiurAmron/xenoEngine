using System.Collections.Generic;

namespace Vax.Xeno {

    using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    [Serializable]
    public class NpcProto {

        public string name;
        public string spriteName;
        public string bloodColor;

    }

    [Serializable]
    public class NpcData {

        public NpcProto[] npcProtos;

    }

    [Serializable]
    public class BkgdProto {

        public string name;
        public string spriteName;
        public string fogColor;

    }

    [Serializable]
    public class BkgdData {

        public BkgdProto[] bkgdProtos;

    }

    public enum Distance {

        Melee = 0,
        Near = 1,
        Medium = 2,
        Far = 3,
        None = 4,

    }

    public static class DistanceMethods {

        public static Distance add ( this Distance distance, MoveDirection moveDirection ) {
            return (Distance) ( (int) distance + (int) moveDirection );
        }

    }

    public enum State {

        Idle = 0,
        Move = 1,
        AttackRanged = 2,
        AttackMelee = 3,

    }

    public enum MoveDirection {

        Approach = -1,
        None = 0,
        Escape = 1,

    }

    public class App : MonoBehaviour {

        public static App app; // singleton

        // // //

        public GameObject bkgd = null;
        public GameObject bkgdOverlay = null;

        public GameObject npc = null;

        public GameObject fog = null;

        // // //

        public NpcData npcData = null;
        public BkgdData bkgdData = null;

        // // //

        public const int COUNTER_MAX = 36;

        public int attackCounter = 0;
        public int moveCounter = 0;
        public MoveDirection currentMoveDirection = MoveDirection.None;

        public Distance distance = Distance.Far;

        public State state = State.Idle;

        protected readonly Dictionary<State, Action> stateMap;

        // // //

        public App () {
            stateMap = new Dictionary<State, Action> {
                    [State.AttackMelee] = updateAttackMelee,
                    [State.AttackRanged] = updateAttackRanged,
                    [State.Move] = updateMove,
                    [State.Idle] = updateIdle,
                }
                ;
        }

        // // //

        // MonoBehaviour implementation

        protected void Awake () {
            if ( app != null ) {
                throw new InvalidOperationException( "app already initialized" );
            }

            app = this;

            npcData = Utils.loadFromJsonResource<NpcData>( "npcs" );
            bkgdData = Utils.loadFromJsonResource<BkgdData>( "bkgds" );
        }

        protected GameObject createOverlay ( string resourceName, Color color, string sortingLayerName = "Overlay" ) {
            GameObject go = new GameObject( resourceName );

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>( resourceName );
            if ( sr.sprite == null ) {
                throw new FileNotFoundException();
            }

            sr.color = color;
            sr.sortingLayerName = sortingLayerName;

            go.scaleToScreen();

            return go;
        }

        protected void Start () {
            bkgdOverlay = createOverlay( "blood", new Color( 1.0f, 0.0f, 0.0f, 0.0f ) );
            fog = createOverlay( "vines", new Color( 1.0f, 1.0f, 1.0f, 1.0f ), "Fog" );
        }

        protected void Update () {
            stateMap[state]();
        }

        // implementations

        protected void updateIdle () {
            // nothing currently; put idle animations here later on
        }

        protected void updateMove () {
            if ( currentMoveDirection == 0 ) {
                return;
            }

            if ( moveCounter >= COUNTER_MAX ) {
                distance += (int) currentMoveDirection;
                GameObject.Find( "DistanceSelector" ).GetComponent<Dropdown>().value = (int) distance;
                if ( distance == Distance.None ) {
                    if ( app.npc ) {
                        Destroy( app.npc );
                        GameObject.Find( "NpcSelector" ).GetComponent<Dropdown>().value = 0;
                    }
                }
                moveCounter = 0;
                currentMoveDirection = MoveDirection.None;
                state = State.Idle;
                return;
            }

            moveCounter++;
            updateNpcMove();
        }

        protected void updateAttackMelee () {
            if ( attackCounter <= -COUNTER_MAX ) {
                state = State.Idle;
                return;
            }

            float ratioInv = 1.0f * Math.Abs( attackCounter ) / COUNTER_MAX; // 1 -> 0 -> 1
            float ratio = 1.0f - ratioInv; // 0 -> 1 -> 0

            if ( bkgd != null ) {
                bkgd.setSpriteColor( null, ratioInv, ratioInv ); // go to red
            }

            if ( bkgdOverlay != null ) {
                bkgdOverlay.setSpriteColor( null, ratioInv, ratioInv, ratioInv ); // go to red
            }

            if ( npc != null ) {
                npc.setSpriteColor( null, ratioInv, ratioInv, ratioInv ); // go to red

                updateNpcMove( ratio );
                Transform t = npc.transform;
                Vector3 lea = t.localEulerAngles;
                lea.z = 45.0f * ratio;
                t.localEulerAngles = lea;
            }

            attackCounter--;
        }

        protected void updateAttackRanged () {
            if ( attackCounter <= -COUNTER_MAX ) {
                state = State.Idle;
                return;
            }

            attackCounter--;
        }

        public bool initiateMove ( MoveDirection moveDirection ) {
            if ( npc == null || state != State.Idle ) {
                return false;
            }
            Distance requestedDistance = distance.add( moveDirection );
            if ( requestedDistance > Distance.None || requestedDistance < Distance.Melee ) {
                return false;
            }

            currentMoveDirection = moveDirection;

            state = State.Move;

            return true;
        }

        public bool npcClick () {
            if ( state != State.Idle ) {
                return false;
            }

            attackCounter = 36;

            state = ( distance == Distance.Melee ) ? State.AttackMelee : State.AttackRanged;

            return true;
        }

        public void updateNpcMove ( float ratio = 0 ) {
            float realDistance = (int) distance +
                                 (int) currentMoveDirection * 1.0f * moveCounter / COUNTER_MAX;

            fog.setSpriteColor( null, null, null, 0.1f * realDistance );

            fog.setSpriteColor( null, null, null, 0.1f * realDistance );

            fog.scaleToScreen( 5.0f - realDistance );

            float shadowFactorNpc = 1.0f - 0.3f * realDistance;
            npc.setSpriteColor( shadowFactorNpc, shadowFactorNpc, shadowFactorNpc );

            if ( bkgd != null ) {
                float shadowFactor = 1.0f - 0.25f * realDistance;
                if ( shadowFactor < 0.25f ) {
                    shadowFactor = 0.25f;
                }
                bkgd.setSpriteColor( shadowFactor, shadowFactor, shadowFactor );
            }

            float scaleFactor = 0.2f * ( 4.0f + 4.0f * ratio - realDistance );
            npc.transform.localScale = new Vector3( scaleFactor, scaleFactor, 1 );
        }

    }

}