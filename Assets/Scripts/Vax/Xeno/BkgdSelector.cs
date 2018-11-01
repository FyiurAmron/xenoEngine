namespace Vax.Xeno {

using System.Linq;
using System.Collections.Generic;
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

    protected void onValueChanged( int val, Dropdown dd ) {
        if ( val == 0 ) {
            return;
        }

        App app = App.app;

        BkgdProto bkgdName = app.bkgdMap[dd.captionText.text];

        var sprites = bkgdName.sprites;

        if ( sprites.TryGetValue( "1", out var bkgdOverlayNear ) ) {
            app.bkgdOverlayNear.createGameObject(
                "Bkgd/" + bkgdOverlayNear,
                new Color( 1.0f, 1.0f, 1.0f, 1.0f )
            );
        }

        if ( sprites.TryGetValue( "2", out var bkgdOverlayFar ) ) {
            app.bkgdOverlayFar.createGameObject(
                "Bkgd/" + bkgdOverlayFar,
                new Color( 1.0f, 1.0f, 1.0f, 1.0f )
            );
        }

        app.bkgd.createGameObject(
            "Bkgd/" + sprites["3"]
        );
    }
}

}