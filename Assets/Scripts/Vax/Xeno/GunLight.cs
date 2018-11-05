namespace Vax.Xeno {

using UnityEngine;

public class GunLight {
    public GameObject gameObject;
    public Light light;
    public float range;

    public bool enabled {
        get => light.enabled;
        set => light.enabled = value;
    }

    public const float MINIMUM_DISTANCE = 0.5f;
    protected const float RANGE_OFFSET = 0.02f;
    
    public GunLight( float range = 10.0f ) {
        this.range = range;
        gameObject = new GameObject( GetType().Name );
        gameObject.transform.position = new Vector3( 0, 0, 0 );

        light = gameObject.AddComponent<Light>(); // point light
        light.color = new Color( 1.0f, 0.75f, 0.6f );
        light.range = range + RANGE_OFFSET;
        light.intensity = 20;
        light.enabled = false;
    }

    public GunLight setPosition( float? x, float? y, float? z = null ) {

        Vector3 pos = gameObject.transform.position;
        
        if ( x != null ) {
            pos.x = (float) x;
        }

        if ( y != null ) {
            pos.y = (float) y;
        }

        if ( z != null ) {
            pos.z = (float) z;
        }
        
        gameObject.transform.position = pos;

        return this;
    }
}

}