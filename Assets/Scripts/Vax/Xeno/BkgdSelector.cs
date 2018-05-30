namespace Vax.Xeno {

    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    public class BkgdSelector : MonoBehaviour {

        public static readonly string[] BKGD_NAMES = new string[] {
            "- bkgd -", "cave", "forest", "mountain", "swamp", "water"
        }; // TODO move to JSON

        public static readonly List<string> BKGD_NAMES_LIST = new List<string>( BKGD_NAMES );

        protected void Start () {
            Dropdown dd = gameObject.GetComponent<Dropdown>();
            dd.AddOptions( BKGD_NAMES_LIST );
            dd.onValueChanged.AddListener( ( val ) => onValueChanged( val, dd ) );
        }

        protected void Update () {
        }

        protected void onValueChanged ( int val, Dropdown dd ) {
            if ( val == 0 ) {
                return;
            }

            App app = GameObject.Find( "App" ).GetComponent<App>();
            if ( app.bkgd != null ) {
                Destroy( app.bkgd );
            }

            GameObject go = new GameObject();

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            sr.sprite = Resources.Load( "Bkgd/" + dd.captionText.text, typeof(Sprite) ) as Sprite;
            if ( sr.sprite == null ) {
                throw new FileNotFoundException();
            }

            sr.sortingLayerName = "Bkgd";

            Vector3 size = sr.sprite.bounds.size;

            float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
            float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

            Vector3 scale = go.transform.localScale;
            scale.x = worldScreenWidth / size.x;
            scale.y = worldScreenHeight / size.y;
            go.transform.localScale = scale;

            app.bkgd = go;
        }

    }

}