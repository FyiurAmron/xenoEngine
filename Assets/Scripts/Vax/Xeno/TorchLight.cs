namespace Vax.Xeno {

using UnityEngine;

public class TorchLight {
    public GameObject gameObject;
    public Light light;
    public float range;

    public const float MINIMUM_DISTANCE = 0.5f;
    protected const float RANGE_OFFSET = 0.02f;
    
    public TorchLight( float range = 5.0f ) {
        this.range = range;
        gameObject = new GameObject( GetType().Name );
        gameObject.transform.position = new Vector3( 0, 0, -range );

        light = gameObject.AddComponent<Light>(); // point light
        light.color = new Color( 0.75f, 0.75f, 0.75f );
        light.range = range + RANGE_OFFSET;
        light.intensity = 10;
    }

    public TorchLight setPosition( float? x, float? y, float? z = null ) {

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