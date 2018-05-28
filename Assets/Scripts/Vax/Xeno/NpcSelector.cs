namespace Vax.Xeno {

    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    public class NpcSelector : MonoBehaviour {

        public static readonly string[] NPC_NAMES = new string[] {
            "- npc -", "aquan", "bisape", "crusher", "eleboid", "ivy_5"
        };

        public static readonly List<string> NPC_NAMES_LIST = new List<string>(NPC_NAMES);

        protected void Start() {
            Dropdown dd = gameObject.GetComponent<Dropdown>();
            dd.AddOptions(NPC_NAMES_LIST);
            dd.onValueChanged.AddListener((val) => onValueChanged(val, dd));
        }

        protected void Update() {
        }

        protected void onValueChanged(int val, Dropdown dd) {
            if (val == 0) {
                return;
            }

            App app = GameObject.Find("App").GetComponent<App>();
            if (app.npc != null) {
                Destroy(app.npc);
            }

            GameObject go = new GameObject();

            BoxCollider2D bc2D = go.AddComponent<BoxCollider2D>();
            go.AddComponent<NpcClickHandler>();

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            sr.sprite = Resources.Load("Npcs/" + dd.captionText.text, typeof(Sprite)) as Sprite;
            if (sr.sprite == null) {
                throw new FileNotFoundException();
            }

            sr.sortingLayerName = "Npc";

            Vector3 bounds = sr.bounds.size;
            Vector3 scale = go.transform.lossyScale;

            bc2D.size = new Vector3(
                bounds.x / scale.x,
                bounds.y / scale.y,
                bounds.z / scale.z
            );

            GameObject.Find("DistanceSelector").GetComponent<Dropdown>().value = (int) App.Distance.None;

            app.npc = go;

            app.updateNpcScale();

            app.approach();
        }

    }

}