namespace Vax.Xeno {

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DistanceSelector : MonoBehaviour {
    public static readonly List<string> DISTANCE_NAMES_LIST = new List<string>(
        Enum.GetNames( typeof(Distance) ) );

    protected void Start() {
        Dropdown dd = gameObject.GetComponent<Dropdown>();
        dd.AddOptions( DISTANCE_NAMES_LIST );
        dd.onValueChanged.AddListener( ( val ) => {
            App.app.handleClick( ClickContext.Ui );
            onValueChanged( val, dd );
        } );
    }

    protected void Update() {
    }

    protected void onValueChanged( int val, Dropdown dd ) {
        App app = GameObject.Find( "App" ).GetComponent<App>();
        if ( app.currentMoveDirection != 0 ) {
            gameObject.GetComponent<Dropdown>().value = (int) app.distance;

            return;
        }

        app.distance = (Distance) val;

        if ( app.npcEntity == null ) {
            return;
        }

        GameObject npc = app.npcEntity.gameObject;

        app.updateNpcMove();

        npc.setBoundsFromSprite();
    }
}

}