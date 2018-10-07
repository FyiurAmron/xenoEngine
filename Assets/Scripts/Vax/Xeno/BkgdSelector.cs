namespace Vax.Xeno {

using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class BkgdSelector : MonoBehaviour {
    public readonly List<string> bkgdNameList = new List<string> {
        "- bkgd -"
    };

    protected void Start() {
        var spriteNames = App.app.bkgdConfig.bkgdProtos.Select( x => x.name );
        bkgdNameList.AddRange( spriteNames );

        Dropdown dd = gameObject.GetComponent<Dropdown>();
        dd.AddOptions( bkgdNameList );
        dd.onValueChanged.AddListener( ( val ) => {
            App.app.handleClick( ClickContext.Ui );
            onValueChanged( val, dd );
        } );
    }

    protected void Update() {
    }

    protected GameObject createBkgd( App app, string bkgdName ) {
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

    protected void onValueChanged( int val, Dropdown dd ) {
        if ( val == 0 ) {
            return;
        }

        App app = GameObject.Find( "App" ).GetComponent<App>();

        if ( app.bkgdOverlayNear != null ) {
            Destroy( app.bkgdOverlayNear );
        }
        if ( app.bkgdOverlayFar != null ) {
            Destroy( app.bkgdOverlayFar );
        }
        if ( app.bkgd != null ) {
            Destroy( app.bkgd );
        }

        BkgdProto bkgdName = App.app.bkgdMap[dd.captionText.text];

        var sprites = bkgdName.sprites;
       
        if ( sprites.TryGetValue( "1", out var bkgdOverlayNear ) ) {
            App.app.bkgdOverlayNear = App.app.createOverlay(
                "Bkgd/" + bkgdOverlayNear,
                new Color( 1.0f, 1.0f, 1.0f, 1.0f ),
                -1, "Fog" );
        }
        
        if ( sprites.TryGetValue( "2", out var bkgdOverlayFar ) ) {
            App.app.bkgdOverlayFar = App.app.createOverlay(
                "Bkgd/" + bkgdOverlayFar,
                new Color( 1.0f, 1.0f, 1.0f, 1.0f ),
                -2, "Fog" );
        }
        
        createBkgd( app, sprites["3"] );
    }
}

}