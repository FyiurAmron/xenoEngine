using System.Xml.Schema;
using UnityEngine;

namespace Vax.Xeno {

    public static class Utils {

        public static float clamp ( this float f, float min, float max ) {
            return ( f <= min )
                ? min
                : ( f >= max
                    ? max
                    : f );
        }

        public static float clampNorm ( this float f ) {
            return clamp( f, 0.0f, 1.0f );
        }

        
        public static void setSpriteColor ( this GameObject gameObject,
            float? red, float? green, float? blue, float? alpha = null ) {
            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            Color c = sr.color;
            if ( red != null ) {
                c.r = clampNorm( red.Value );
            }
            if ( green != null ) {
                c.g = clampNorm( green.Value );
            }
            if ( blue != null ) {
                c.b = clampNorm( blue.Value );
            }
            if ( alpha != null ) {
                c.a = clampNorm( alpha.Value );
            }
            sr.color = c;
        }

        public static void scaleToScreen ( this GameObject gameObject, float scaleFactor = 1.0f ) {
            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            Vector3 size = sr.sprite.bounds.size;

            float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
            float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

            Transform transform = gameObject.transform;
            Vector3 scale = transform.localScale;
            scale.x = scaleFactor * worldScreenWidth / size.x;
            scale.y = scaleFactor * worldScreenHeight / size.y;
            transform.localScale = scale;
        }

        public static void setBoundsFromSprite ( this GameObject gameObject ) {
            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            BoxCollider2D bc2D = gameObject.GetComponent<BoxCollider2D>();

            Vector3 bounds = sr.bounds.size;
            Vector3 scale = gameObject.transform.lossyScale;

            bc2D.size = new Vector3(
                bounds.x / scale.x,
                bounds.y / scale.y,
                bounds.z / scale.z
            );
        }

        public static T loadFromJsonResource<T> ( string jsonName ) {
            string jsonText = Resources.Load<TextAsset>( jsonName ).text;
            return JsonUtility.FromJson<T>( jsonText );
        }

    }

}