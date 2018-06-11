namespace Vax.Xeno {

    using UnityEngine;
    using UnityEngine.UI;

    public class EscapeButton : MonoBehaviour {

        protected void Start () {
            Button b = gameObject.GetComponent<Button>();
            b.onClick.AddListener( () => {
                App.app.handleClick( ClickContext.Ui );
                App.app.initiateMove( MoveDirection.Escape );
            } );
        }

    }

}