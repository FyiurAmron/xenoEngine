﻿namespace Vax.Xeno {

    using System;
    using System.IO;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

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

    public enum ClickContext {

        Npc = 0,
        Ui = 1,

    }

    // // //

    public class App : MonoBehaviour {

        public static App app = null; // singleton

        // // //

        public GameObject bkgd = null;
        public GameObject bkgdOverlay = null;

        public GameObject npc = null;

        public GameObject bkgdNear = null;

        // // //

        public NpcConfig npcConfig = null;
        public BkgdConfig bkgdConfig = null;
        public SfxConfig sfxConfig = null;

        public AudioSource audioSource = null;

        public Dictionary<string, BkgdProto> bkgdMap = null;
        public Dictionary<string, SfxProto> sfxMap = null;

        // // //

        public const int COUNTER_MAX = 36;

        public int attackCounter = 0;
        public int moveCounter = 0;
        public MoveDirection currentMoveDirection = MoveDirection.None;

        public Distance distance = Distance.Far;

        public State state = State.Idle;

        protected readonly Dictionary<State, Action> stateMap = null;

        protected readonly Dictionary<ClickContext, Func<bool>> clickHandlers = null;

        public bool clickHandled = false;

        public readonly List<GameObject> attackDebris = new List<GameObject>();

        // // //

        public App () {
            stateMap = new Dictionary<State, Action> {
                    [State.AttackMelee] = updateAttackMelee,
                    [State.AttackRanged] = updateAttackRanged,
                    [State.Move] = updateMove,
                    [State.Idle] = updateIdle,
                }
                ;
            clickHandlers = new Dictionary<ClickContext, Func<bool>> {
                    [ClickContext.Npc] = clickNpc,
                    [ClickContext.Ui] = clickUi,
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

            npcConfig = Utils.loadFromJsonResource<NpcConfig>( "npc" );
            bkgdConfig = Utils.loadFromJsonResource<BkgdConfig>( "bkgd" );
            sfxConfig = Utils.loadFromJsonResource<SfxConfig>( "sfx" );

            sfxMap = sfxConfig.toSfxMap();
            bkgdMap = bkgdConfig.toBkgdMap();
        }

        public GameObject createOverlay ( string resourceName, Color color, string sortingLayerName = "Overlay" ) {
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
            audioSource = gameObject.AddComponent<AudioSource>();

            bkgdOverlay = createOverlay( "blood", new Color( 1.0f, 0.0f, 0.0f, 0.0f ) );
        }

        protected void Update () {
            stateMap[state]();
            clickHandled = false;
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
                bkgdOverlay.setSpriteColor( null, ratioInv, ratioInv, ratio ); // go to red
            }

            if ( npc != null ) {
                npc.setSpriteColor( null, ratioInv, ratioInv, ratioInv ); // go to red

                float scaleFactor = getNpcScaleFactor( ratio );
                npc.transform.localScale = new Vector3( scaleFactor, scaleFactor, 1 );

                Transform t = npc.transform;
                Vector3 lea = t.localEulerAngles;
                lea.z = 45.0f * ratio;
                t.localEulerAngles = lea;
            }

            attackCounter--;
        }

        protected void updateAttackRanged () {
            if ( attackCounter <= -COUNTER_MAX ) {
                foreach ( var v in attackDebris ) {
                    Destroy( v );
                }
                state = State.Idle;
                return;
            }

            if ( attackCounter > 0 ) {
                if ( attackCounter % 10 == 0 ) {
                    GameObject go = new GameObject();
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

                    sr.sprite = Resources.Load<Sprite>( "splat" );
                    if ( sr.sprite == null ) {
                        throw new FileNotFoundException();
                    }
                    float splatScaleFactor = UnityEngine.Random.Range( 0.2f, 0.4f ) * getNpcScaleFactor();
                    Transform t = go.transform;
                    t.localScale = new Vector3( splatScaleFactor, splatScaleFactor, 1 );
                    t.position = new Vector3(
                        UnityEngine.Random.Range( -0.5f, 0.5f ),
                        UnityEngine.Random.Range( -1.5f, 1.5f ),
                        1 );

                    Vector3 lea = t.localEulerAngles;
                    lea.z = UnityEngine.Random.Range( 0.0f, 360.0f );
                    t.localEulerAngles = lea;

                    go.setSpriteColor( 0.2f, 0.8f, 0.4f, 0.8f );

                    sr.sortingLayerName = "NpcDeco";

                    attackDebris.Add( go );
                }
            }
            if ( attackCounter < 30 && attackCounter >= -1 ) {
                float ratio = ( attackCounter == -1 ) ? 0 : -0.01f * ( attackCounter % 10 );

                float npcScaleFactor = getNpcScaleFactor( ratio );
                npc.transform.localScale = new Vector3( npcScaleFactor, npcScaleFactor, 1 );
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

        public bool handleClick ( ClickContext clickContext ) {
            if ( clickHandled ) {
                return false;
            }

            clickHandled = clickHandlers[clickContext]();
            return clickHandled;
        }

        public bool clickUi () {
            return true;
        }

        public bool clickNpc () {
            if ( state != State.Idle
                 || EventSystem.current.IsPointerOverGameObject() ) {
                // 2nd condition is needed since UI is often on touch/mouse up, but other are on touch/mouse down
                return false;
            }

            attackCounter = 36;

            // TODO differentiate weapons/enemy

            if ( distance == Distance.Melee ) {
                sfxMap["melee"].playOneShot();
                state = State.AttackMelee;
            } else {
                sfxMap["shot"].playOneShot();
                state = State.AttackRanged;
            }

            return true;
        }

        protected float getRealDistance () {
            return (int) distance +
                   (int) currentMoveDirection * 1.0f * moveCounter / COUNTER_MAX;
        }

        protected float getNpcScaleFactor ( float ratio = 0.0f ) {
            return 0.2f * ( 4.0f + 4.0f * ratio - getRealDistance() );
        }

        public void updateNpcMove ( float ratio = 0.0f ) {
            float realDistance = getRealDistance();

            if ( bkgdNear != null ) {
                bkgdNear.setSpriteColor( null, null, null, 0.1f * realDistance );

                bkgdNear.scaleToScreen( 5.0f - realDistance );
            }

            if ( bkgd != null ) {
                //bkgd.setSpriteColor( null, null, null, 0.1f * realDistance );

                bkgd.scaleToScreen( 3.0f - 0.5f * realDistance );
            }
            
            float shadowFactorNpc = 1.0f - 0.3f * realDistance;
            npc.setSpriteColor( shadowFactorNpc, shadowFactorNpc, shadowFactorNpc );

            if ( bkgd != null ) {
                float shadowFactor = 1.0f - 0.25f * realDistance;
                if ( shadowFactor < 0.25f ) {
                    shadowFactor = 0.25f;
                }
                bkgd.setSpriteColor( shadowFactor, shadowFactor, shadowFactor );
            }

            float scaleFactor = getNpcScaleFactor( ratio );
            npc.transform.localScale = new Vector3( scaleFactor, scaleFactor, 1 );
        }

    }

}