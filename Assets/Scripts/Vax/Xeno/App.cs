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

        public GameObject bkgd = null;
        public GameObject bkgdOverlay = null;
        public GameObject npc = null;

        public NpcData npcData = null;

        public const int COUNTER_MAX = 36;

        public int attackCounter = 0;
        public int moveCounter = 0;
        public MoveDirection currentMoveDirection = MoveDirection.None;

        public Distance distance = Distance.Far;

        public State state = State.Idle;

        protected readonly Dictionary<State, Action> stateMap;

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

            string jsonText = Resources.Load<TextAsset>( "npcs" ).text;
            npcData = JsonUtility.FromJson<NpcData>( jsonText );
        }

        protected void Start () {

            GameObject go = new GameObject();

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>( "blood" );
            if ( sr.sprite == null ) {
                throw new FileNotFoundException();
            }

            Color c = sr.color;
            c.b = 0;
            c.g = 0;
            c.a = 0;
            sr.color = c;
            sr.sortingLayerName = "Overlay";

            Vector3 size = sr.sprite.bounds.size;

            float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
            float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

            Vector3 scale = go.transform.localScale;
            scale.x = worldScreenWidth / size.x;
            scale.y = worldScreenHeight / size.y;
            go.transform.localScale = scale;

            bkgdOverlay = go;
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
            updateNpcScale();
        }

        protected void updateAttackMelee () {
            if ( attackCounter <= -COUNTER_MAX ) {
                state = State.Idle;
                return;
            }

            float ratioInv = 1.0f * Math.Abs( attackCounter ) / COUNTER_MAX; // 1 -> 0 -> 1
            float ratio = 1.0f - ratioInv; // 0 -> 1 -> 0

            Color c;
            SpriteRenderer sr;

            if ( bkgd != null ) {
                sr = bkgd.GetComponent<SpriteRenderer>();
                c = sr.color;
                c.b = ratioInv;
                c.g = ratioInv;
                sr.color = c;
            }

            if ( bkgdOverlay != null ) {
                sr = bkgdOverlay.GetComponent<SpriteRenderer>();
                c = sr.color;
                c.a = ratio;
                c.b = ratioInv;
                c.g = ratioInv;
                sr.color = c;
            }

            if ( npc != null ) {
                sr = npc.GetComponent<SpriteRenderer>();
                c = sr.color;
                c.a = ratioInv;
                c.b = ratioInv;
                c.g = ratioInv;
                sr.color = c;

                updateNpcScale( ratio );
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
            if ( requestedDistance > Distance.Far || requestedDistance < Distance.Melee ) {
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

        public void updateNpcScale ( float ratio = 0 ) {
            float scaleFactor = 0.1f * ( 4.0f + 4.0f * ratio - (int) app.distance -
                                         (int) currentMoveDirection * 1.0f * moveCounter / COUNTER_MAX );
            npc.transform.localScale = new Vector3( scaleFactor, scaleFactor, 1 );
        }

    }

}