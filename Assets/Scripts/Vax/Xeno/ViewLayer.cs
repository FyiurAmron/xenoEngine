using System.IO;
using UnityEngine;

namespace Vax.Xeno {

public class ViewLayer {
    protected readonly int sortingOrder;
    protected readonly string sortingLayerName;
    protected readonly float baseScaleFactor;

    protected GameObject gameObject;
    protected SpriteRenderer spriteRenderer;
    protected Sprite sprite;
    protected bool active;

    public float scaleFactor { get; set; } = 1.0f;

    public ViewLayer( string sortingLayerName = "Overlay", int sortingOrder = 0, float baseScaleFactor = 1.0f ) {
        this.sortingOrder = sortingOrder;
        this.sortingLayerName = sortingLayerName;
        this.baseScaleFactor = baseScaleFactor;
    }

    public ViewLayer createGameObject( string resourceName, Color? color = null ) {
        destroy(); // cleanup old state

        gameObject = new GameObject( resourceName );
        sprite = Resources.Load<Sprite>( resourceName );
        if ( sprite == null ) {
            throw new FileNotFoundException();
        }

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        if ( color != null ) {
            spriteRenderer.color = color.Value;
        }

        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.sortingLayerName = sortingLayerName;

        active = true;

        update();
       
        return this;
    }

    public ViewLayer setColor( Color color ) {
        if ( !active ) {
            return this;
        }

        spriteRenderer.color = color;

        return this;
    }

    public ViewLayer setColor( float? red, float? green, float? blue, float? alpha = null ) {
        if ( !active ) {
            return this;
        }

        spriteRenderer.setColor( red, green, blue, alpha );

        return this;
    }

    public ViewLayer update() {
        if ( !active ) {
            return this;
        }

        gameObject.scaleToScreen( baseScaleFactor * scaleFactor );

        return this;       
    }

    public ViewLayer setPosition( float x, float y, float z ) {
        if ( !active ) {
            return this;
        }

        gameObject.transform.position = new Vector3( x, y, z );

        return this;
    }

    /// <summary>
    /// idempotent
    /// </summary>
    /// <returns></returns>
    public ViewLayer destroy() {
        if ( !active ) {
            return this;
        }

        Object.Destroy( gameObject );
        gameObject = null;
        spriteRenderer = null;
        sprite = null;
        active = false;

        return this;
    }

    public bool isActive() {
        return active;
    }
}

}