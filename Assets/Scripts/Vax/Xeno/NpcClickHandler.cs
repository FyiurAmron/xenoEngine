namespace Vax.Xeno {

    using UnityEngine;

    public class NpcClickHandler : MonoBehaviour {

        protected void OnMouseDown () {
            App.app.npcClick();
        }

    }

}