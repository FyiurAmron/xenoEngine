using System.Linq;

namespace Vax.Xeno {

    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    public class BkgdSelector : MonoBehaviour {

        public readonly List<string> bkgdNameList = new List<string> {
            "- bkgd -"
        };

        protected void Start () {
            var spriteNames = App.app.bkgdData.bkgdProtos.Select( x => x.spriteName );
            bkgdNameList.AddRange( spriteNames );

            Dropdown dd = gameObject.GetComponent<Dropdown>();
            dd.AddOptions( bkgdNameList );
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

            string bkgdName = dd.captionText.text;

            GameObject go = new GameObject( bkgdName );

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            sr.sprite = Resources.Load( "Bkgd/" + bkgdName, typeof(Sprite) ) as Sprite;
            if ( sr.sprite == null ) {
                throw new FileNotFoundException();
            }

            sr.sortingLayerName = "Bkgd";

            go.scaleToScreen();
            
            app.bkgd = go;
        }

    }

}