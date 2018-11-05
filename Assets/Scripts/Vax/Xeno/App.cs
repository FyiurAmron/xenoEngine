namespace Vax.Xeno {

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Entities;
using Random = UnityEngine.Random;

public enum Distance {
    Melee = 0,
    Near = 1,
    Medium = 2,
    Far = 3,
    None = 4,
}

public static class DistanceMethods {
    public static readonly Dictionary<Distance, float> DISTANCE_MAP = new Dictionary<Distance, float> {
        [Distance.Melee] = 0.0f,
        [Distance.Near] = 0.3f,
        [Distance.Medium] = 0.7f,
        [Distance.Far] = 0.85f,
        [Distance.None] = 1.0f,
    };

    public static Distance add( this Distance distance, MoveDirection moveDirection ) {
        return (Distance) ( (int) distance + (int) moveDirection );
    }

    public static float getFactor( this Distance distance ) {
        return DISTANCE_MAP[distance];
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

    public Camera mainCamera = null;
    public Vector3 mainCameraPosition;

    public ViewLayer overlay = null;
    public ViewLayer bkgd = null;
    public ViewLayer bkgdOverlayNear = null;
    public ViewLayer bkgdOverlayFar = null;

    public TorchLight torchLight = null;
    public GunLight gunLight = null;
    public Color ambientLight;

    public const float OVERLAY_UPSCALE_FACTOR = 0.1f;
    public const float ROOT_SCALE_FACTOR = 1.0f; //0.5f;

    public const float SCREEN_SHAKE_AMOUNT = 0.5f;

    public NpcEntity npcEntity = null;

    // // //

    public NpcConfig npcConfig = null;
    public BkgdConfig bkgdConfig = null;
    public SfxConfig sfxConfig = null;

    public AudioSource audioSource = null;

    public Dictionary<string, BkgdProto> bkgdMap = null;
    public Dictionary<string, SfxProto> sfxMap = null;

    // // //

    public const int COUNTER_MAX = 36;

    public int mainCounter;
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

    public App() {
        stateMap = new Dictionary<State, Action> {
            [State.AttackMelee] = updateAttackMelee,
            [State.AttackRanged] = updateAttackRanged,
            [State.Move] = updateMove,
            [State.Idle] = updateIdle,
        };
        clickHandlers = new Dictionary<ClickContext, Func<bool>> {
            [ClickContext.Npc] = clickNpc,
            [ClickContext.Ui] = clickUi,
        };
    }

    // // //

    // MonoBehaviour implementation

    protected void Awake() {
        if ( app != null ) {
            throw new InvalidOperationException( "app already initialized" );
        }

        app = this;

        npcConfig = Utils.loadFromJsonResource<NpcConfig>( "npc" );
        bkgdConfig = Utils.loadFromJsonResource<BkgdConfig>( "bkgd" );
        sfxConfig = Utils.loadFromJsonResource<SfxConfig>( "sfx" );

        sfxMap = sfxConfig.toSfxMap();
        bkgdMap = bkgdConfig.toBkgdMap();

        ambientLight = new Color( 24f / 256, 24f / 256, 24f / 256 );
    }

    protected void Start() {
        audioSource = gameObject.AddComponent<AudioSource>();

        mainCamera = Camera.main;
        if ( mainCamera == null ) {
            throw new InvalidOperationException();
        }

        mainCameraPosition = mainCamera.transform.position;

        overlay = new ViewLayer();
        overlay.createGameObject( "blood", new Color( 1.0f, 0.0f, 0.0f, 0.0f ) );
        bkgd = new ViewLayer( "Bkgd", 0, ROOT_SCALE_FACTOR );
        bkgdOverlayFar = new ViewLayer( "Fog", -2, ROOT_SCALE_FACTOR * ( 1.0f + 1.0f * OVERLAY_UPSCALE_FACTOR ) );
        bkgdOverlayNear = new ViewLayer( "Fog", -1, ROOT_SCALE_FACTOR * ( 1.0f + 2.0f * OVERLAY_UPSCALE_FACTOR ) );

        torchLight = new TorchLight();
        gunLight = new GunLight();

        mainCounter = 0;
    }

    protected void Update() {
        stateMap[state](); // process current state

        RenderSettings.ambientLight = ambientLight;

        float baseSize = mainCamera.orthographicSize * Math.Max( 1.0f, mainCamera.aspect );

        Vector3 mousePos = Input.mousePosition;
        float baseX = ( mousePos.x / mainCamera.pixelWidth ).toNormCoord() * baseSize;
        float baseY = ( mousePos.y / mainCamera.pixelHeight ).toNormCoord() * baseSize;
        float overlayX = baseX * OVERLAY_UPSCALE_FACTOR;
        float overlayY = baseY * OVERLAY_UPSCALE_FACTOR;

        bkgd.update();

        bkgdOverlayFar.setPosition( overlayX, overlayY, 0 );
        bkgdOverlayFar.update();

        bkgdOverlayNear.setPosition( 2 * overlayX, 2 * overlayY, 0 );
        bkgdOverlayNear.update();

        torchLight.setPosition( baseX, baseY );

        clickHandled = false;

        mainCamera.transform.position = mainCameraPosition;

        mainCounter++;
    }

    // implementations

    protected void updateIdle() {
        // nothing currently; put idle animations here later on
    }

    protected void updateMove() {
        if ( currentMoveDirection == 0 ) {
            return;
        }

        if ( moveCounter >= COUNTER_MAX ) {
            distance += (int) currentMoveDirection;
            GameObject.Find( "DistanceSelector" ).GetComponent<Dropdown>().value = (int) distance;
            if ( distance == Distance.None ) {
                if ( app.npcEntity != null ) {
                    app.npcEntity.destroy();
                    app.npcEntity = null;
                    GameObject.Find( "NpcSelector" ).GetComponent<Dropdown>().value = 0;
                }
            }

            moveCounter = 0;
            currentMoveDirection = MoveDirection.None;
            state = State.Idle;
            return;
        }

        moveCounter++;

        updateBkgdMove();
        updateNpcMove();

        float newLightZ = -getNpcDistanceFactor() * torchLight.range - TorchLight.MINIMUM_DISTANCE;
        torchLight.setPosition( null, null, newLightZ );
        gunLight.setPosition( null, null, newLightZ );
    }

    protected void updateAttackMelee() {
        if ( attackCounter <= -COUNTER_MAX ) {
            state = State.Idle;
            return;
        }

        float ratioInv = 1.0f * Math.Abs( attackCounter ) / COUNTER_MAX; // 1 -> 0 -> 1
        float ratio = 1.0f - ratioInv; // 0 -> 1 -> 0

        bkgd.setColor( null, ratioInv, ratioInv ); // go to red
        overlay.setColor( null, ratioInv, ratioInv, ratio ); // go to red

        if ( npcEntity != null ) {
            GameObject npc = npcEntity.gameObject;
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

    protected void updateAttackRanged() {
        if ( attackCounter <= -COUNTER_MAX ) {
            foreach ( GameObject v in attackDebris ) {
                Destroy( v );
            }

            mainCameraPosition.x = 0;
            mainCameraPosition.y = 0;
            
            gunLight.enabled = false; // safety measure

            state = State.Idle;
            return;
        }

        GameObject npcGameObject = npcEntity.gameObject;
        Transform npcTransform = npcGameObject.transform;

        if ( attackCounter > 0 ) {
            switch ( attackCounter % 10 ) {
                case 0: {
                    mainCameraPosition.x += Random.Range( -SCREEN_SHAKE_AMOUNT, SCREEN_SHAKE_AMOUNT );
                    mainCameraPosition.y += Random.Range( -SCREEN_SHAKE_AMOUNT, SCREEN_SHAKE_AMOUNT );

                    GameObject go = new GameObject();
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

                    sr.sprite = Utils.loadResource<Sprite>( "splat" );

                    float splatScaleFactor = Random.Range( 0.2f, 0.4f ) * getNpcScaleFactor();
                    Transform t = go.transform;
                    t.localScale = new Vector3( splatScaleFactor, splatScaleFactor, 1 );
                    t.position = new Vector3(
                        Random.Range( -0.5f, 0.5f ),
                        Random.Range( -1.5f, 1.5f ),
                        1 );

                    Vector3 lea = t.localEulerAngles;
                    lea.z = Random.Range( 0.0f, 360.0f );
                    t.localEulerAngles = lea;

                    ColorUtility.TryParseHtmlString( npcEntity.proto.bloodColor, out Color bloodColor );
                    bloodColor.a = 1.0f;
                    go.setSpriteColor( bloodColor );

                    sr.sortingLayerName = "NpcDeco";

                    attackDebris.Add( go );

                    gunLight.enabled = true;
                    break;
                }
                case 5:
                    gunLight.enabled = false;
                    break;
            }
        } else {
            gunLight.enabled = false;
        }

        mainCameraPosition.x *= 0.9f;
        mainCameraPosition.y *= 0.9f;

        if ( attackCounter < 30 && attackCounter >= -1 ) {
            float ratio = ( attackCounter == -1 ) ? 0 : -0.01f * ( attackCounter % 10 );

            float npcScaleFactor = getNpcScaleFactor( ratio );
            npcTransform.localScale = new Vector3( npcScaleFactor, npcScaleFactor, 1f );
        }

        attackCounter--;
    }

    public bool initiateMove( MoveDirection moveDirection ) {
        if ( npcEntity == null || state != State.Idle ) {
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

    public bool handleClick( ClickContext clickContext ) {
        if ( clickHandled ) {
            return false;
        }

        clickHandled = clickHandlers[clickContext]();
        return clickHandled;
    }

    public bool clickUi() {
        return true;
    }

    public bool clickNpc() {
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

    protected float getNpcDistanceFactor() {
        float ratio = 1.0f * moveCounter / COUNTER_MAX;
        float curDist = distance.getFactor();
        float targetDist = distance.add( currentMoveDirection ).getFactor();

        float result = curDist * ( 1.0f - ratio ) + targetDist * ratio;

        return result;
    }

    protected float getBkgdDistance() {
        return (int) distance +
            (int) currentMoveDirection * 1.0f * moveCounter / COUNTER_MAX;
    }

    protected float getNpcScaleFactor( float ratio = 0.0f ) {
        return 0.8f * ( 1.0f + 1.0f * ratio - getNpcDistanceFactor() );
    }

    public void updateBkgdMove() {
        float dist = getBkgdDistance();

        bkgd.scaleFactor = 3.0f - 0.5f * dist;
        bkgdOverlayFar.scaleFactor = 4.0f - 0.75f * dist;
        bkgdOverlayNear.scaleFactor = 5.0f - dist;
    }

    public void updateNpcMove( float ratio = 0.0f ) {
        GameObject npcGameObject = npcEntity.gameObject;
        float scaleFactor = getNpcScaleFactor( ratio );
        npcGameObject.transform.localScale = new Vector3( scaleFactor, scaleFactor, 1 );
    }
}

}