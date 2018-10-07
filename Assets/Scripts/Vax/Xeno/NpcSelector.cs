namespace Vax.Xeno {

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Entities;

public class NpcSelector : MonoBehaviour {
    protected readonly List<string> npcNameList = new List<string> {
        "- npc -"
    };

    protected void Start() {
        var spriteNames = App.app.npcConfig.npcProtos.Keys;
        npcNameList.AddRange( spriteNames );

        Dropdown dd = gameObject.GetComponent<Dropdown>();
        dd.AddOptions( npcNameList );
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
        app.npcEntity?.destroy();

        string npcName = dd.captionText.text;

        NpcEntity npcEntity = new NpcEntity( npcName, app.npcConfig );

        GameObject.Find( "DistanceSelector" ).GetComponent<Dropdown>().value = (int) Distance.None;

        app.npcEntity = npcEntity;
        app.updateNpcMove();
        app.initiateMove( MoveDirection.Approach );
    }
}

}