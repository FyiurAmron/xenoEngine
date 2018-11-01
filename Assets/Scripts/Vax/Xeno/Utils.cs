using System.Diagnostics.CodeAnalysis;
using SmallJson;

namespace Vax.Xeno {

using System;
using UnityEngine;

public static class Utils {
    public static float clamp( this float f, float min, float max ) {
        return ( f <= min )
            ? min
            : ( ( f >= max )
                ? max
                : f );
    }

    public static float clampNorm( this float f ) {
        return clamp( f, 0.0f, 1.0f );
    }

    public static float toNormCoord( this float ratio ) {
        return 2.0f * clampNorm( ratio ) - 1.0f;
    }

    public static void setColor(
        /*this*/ ref Color color,
                 float? red, float? green, float? blue, float? alpha = null ) {
        if ( red != null ) {
            color.r = clampNorm( red.Value );
        }

        if ( green != null ) {
            color.g = clampNorm( green.Value );
        }

        if ( blue != null ) {
            color.b = clampNorm( blue.Value );
        }

        if ( alpha != null ) {
            color.a = clampNorm( alpha.Value );
        }
    }

    public static void setColor(
        this SpriteRenderer spriteRenderer,
        float? red, float? green, float? blue, float? alpha = null ) {
        Color c = spriteRenderer.color;
        // c.setColor( red, green, blue, alpha );
        setColor( ref c, red, green, blue, alpha );
        spriteRenderer.color = c;
    }

    public static void setSpriteColor( this GameObject gameObject, Color color ) {
        gameObject.GetComponent<SpriteRenderer>().color = color;
    }

    public static void setSpriteColor(
        this GameObject gameObject,
        float? red, float? green, float? blue, float? alpha = null ) {
        gameObject.GetComponent<SpriteRenderer>().setColor( red, green, blue, alpha );
    }

    public static void scaleToScreen( this GameObject gameObject, float baseScaleFactor = 1.0f ) {
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        Vector3 size = sr.sprite.bounds.size;

        Camera main = App.app.mainCamera;
        float aspect = Math.Max( main.aspect, 1.0f );
        float verticalSize = main.orthographicSize * 2.0f;

        float scaleFactor = baseScaleFactor * verticalSize * aspect;

        Transform transform = gameObject.transform;
        Vector3 scale = transform.localScale;
        scale.x = scaleFactor / size.x;
        scale.y = scaleFactor / size.y;
        transform.localScale = scale;
    }

    [SuppressMessage( "ReSharper", "CompareOfFloatsByEqualityOperator" )]
    public static void setBoundsFromSprite( this GameObject gameObject ) {
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        BoxCollider2D bc2D = gameObject.GetComponent<BoxCollider2D>();

        Vector3 bounds = sr.bounds.size;
        Vector3 scale = gameObject.transform.lossyScale;

        bc2D.size = new Vector2(
            ( scale.x == 0 ) ? 0 : bounds.x / scale.x,
            ( scale.y == 0 ) ? 0 : bounds.y / scale.y
        );
    }

    public static T loadFromJsonResource<T>( string jsonName, string jsonPath = Paths.CONFIG_PATH ) {
        string jsonText = Resources.Load<TextAsset>( jsonPath + jsonName ).text;
        return Json.parse<T>( jsonText );
    }
}

}