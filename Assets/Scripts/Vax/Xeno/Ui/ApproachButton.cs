namespace Vax.Xeno.Ui {

using UnityEngine;
using UnityEngine.UI;

public class ApproachButton : MonoBehaviour {
    protected void Start() {
        Button b = gameObject.GetComponent<Button>();
        b.onClick.AddListener( () => {
            App.app.handleClick( ClickContext.Ui );
            App.app.initiateMove( MoveDirection.Approach );
        } );
    }
}

}