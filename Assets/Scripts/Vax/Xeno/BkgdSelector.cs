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
            var spriteNames = App.app.bkgdConfig.bkgdProtos.Select( x => x.name );
            bkgdNameList.AddRange( spriteNames );

            Dropdown dd = gameObject.GetComponent<Dropdown>();
            dd.AddOptions( bkgdNameList );
            dd.onValueChanged.AddListener( ( val ) => {
                App.app.handleClick( ClickContext.Ui );
                onValueChanged( val, dd );
            } );
        }

        protected void Update () {
        }

        protected GameObject createBkgd ( App app, string bkgdName ) {
            GameObject go = new GameObject( bkgdName );

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            sr.sprite = Resources.Load( "Bkgd/" + bkgdName, typeof(Sprite) ) as Sprite;
            if ( sr.sprite == null ) {
                throw new FileNotFoundException();
            }

            sr.sortingLayerName = "Bkgd";

            go.scaleToScreen();

            app.bkgd = go;

            return go;
        }

        protected void onValueChanged ( int val, Dropdown dd ) {
            if ( val == 0 ) {
                return;
            }

            App app = GameObject.Find( "App" ).GetComponent<App>();
            if ( app.bkgd != null ) {
                Destroy( app.bkgd );
            }
            if ( app.bkgdNear != null ) {
                Destroy( app.bkgdNear );
            }

            BkgdProto bkgdName = App.app.bkgdMap[dd.captionText.text];

            createBkgd( app, bkgdName.spriteFarName );

            if ( bkgdName.spriteNearName != null ) {
                App.app.bkgdNear = App.app.createOverlay(
                    "Bkgd/" + bkgdName.spriteNearName,
                    new Color( 1.0f, 1.0f, 1.0f, 1.0f ),
                    "Fog" );
            }

        }

    }

}