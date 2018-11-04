namespace Vax.Xeno.Ui {

using UnityEngine;
using UnityEngine.UI;

public class AmbientColorScrollbar : MonoBehaviour {
    // Start is called before the first frame update
    protected void Start() {
        Scrollbar scrollbar = gameObject.GetComponent<Scrollbar>();
        
        switch ( scrollbar.name ) {
            case "AmbientRedScrollbar":
                scrollbar.value = App.app.ambientLight.r; 
                break;
            case "AmbientGreenScrollbar":
                scrollbar.value = App.app.ambientLight.g; 
                break;
            case "AmbientBlueScrollbar":
                scrollbar.value = App.app.ambientLight.b; 
                break;
        }

        scrollbar.onValueChanged.AddListener( ( val ) => {
            App.app.handleClick( ClickContext.Ui );
            onValueChanged( val, scrollbar );
        } );
    }

    // Update is called once per frame
    protected void Update() {
    }

    private void onValueChanged( float value, Scrollbar scrollbar ) {
        switch ( scrollbar.name ) {
            case "AmbientRedScrollbar":
                App.app.ambientLight.r = value;
                break;
            case "AmbientGreenScrollbar":
                App.app.ambientLight.g = value;
                break;
            case "AmbientBlueScrollbar":
                App.app.ambientLight.b = value;
                break;
        }
    }
}

}