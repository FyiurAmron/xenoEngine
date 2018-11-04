namespace Vax.Xeno {

using UnityEngine;

public class FlashLight {
    public GameObject gameObject;
    public Light light;
    public float range;

    public const float MINIMUM_DISTANCE = 0.5f;
    protected const float RANGE_OFFSET = 0.02f;
    
    public FlashLight( float range = 5.0f ) {
        this.range = range;
        gameObject = new GameObject( "FlashLight" );
        gameObject.transform.position = new Vector3( 0, 0, -range );

        light = gameObject.AddComponent<Light>();
        light.color = new Color( 0.75f, 0.75f, 0.75f );
        light.range = range + RANGE_OFFSET;
        light.intensity = 10;
    }

    public FlashLight setPosition( float? x, float? y, float? z = null ) {

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