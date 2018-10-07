using UnityEngine;

namespace Vax.Xeno.Entities {

public class NpcClickHandler : MonoBehaviour {
    protected void OnMouseDown() {
        App.app.handleClick( ClickContext.Npc );
    }
}

}