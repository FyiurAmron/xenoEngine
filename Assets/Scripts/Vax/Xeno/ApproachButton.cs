namespace Vax.Xeno {

    using UnityEngine;
    using UnityEngine.UI;

    public class ApproachButton : MonoBehaviour {

        protected void Start () {
            Button b = gameObject.GetComponent<Button>();
            b.onClick.AddListener( () =>
                App.app.initiateMove( MoveDirection.Approach ) );
        }

    }

}