namespace Vax.Xeno {

    using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    public class App : MonoBehaviour {

        public GameObject bkgd = null;
        public GameObject bkgdOverlay = null;
        public GameObject npc = null;

        public bool wasClicked = false;
        public int attackCounter = 0;
        public int moveCounter = 0;
        public int moveDirection = 0;

        public const int COUNTER_MAX = 36;

        public Distance distance = Distance.Far;

        public static App app; // singleton
        public bool distanceTransition = false;

        public enum Distance {

            Melee = 0,
            Near = 1,
            Medium = 2,
            Far = 3,
            None = 4,

        }

        protected void Start() {
            if (app != null) {
                throw new InvalidOperationException("app already initialized");
            }

            app = this;

            GameObject go = new GameObject();

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load("blood", typeof(Sprite)) as Sprite;
            if (sr.sprite == null) {
                throw new FileNotFoundException();
            }

            Color c = sr.color;
            c.b = 0;
            c.g = 0;
            c.a = 0;
            sr.color = c;
            sr.sortingLayerName = "Overlay";

            Vector3 size = sr.sprite.bounds.size;

            float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
            float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

            Vector3 scale = go.transform.localScale;
            scale.x = worldScreenWidth / size.x;
            scale.y = worldScreenHeight / size.y;
            go.transform.localScale = scale;

            bkgdOverlay = go;
        }

        protected void Update() {
            updateMove();
            updateAttack();
        }

        protected void updateMove() {
            if (moveDirection == 0) {
                return;
            }

            if (moveCounter >= COUNTER_MAX) {
                distance += moveDirection;
                GameObject.Find("DistanceSelector").GetComponent<Dropdown>().value = (int) distance;
                if (distance == Distance.None) {
                    if (app.npc) {
                        Destroy(app.npc);

                        GameObject.Find("NpcSelector").GetComponent<Dropdown>().value = 0;
                    }
                }

                moveCounter = 0;
                moveDirection = 0;

                return;
            }

            moveCounter++;
            updateNpcScale();
        }

        protected void updateAttack() {
            if (!wasClicked) {
                return;
            }

            if (attackCounter <= -COUNTER_MAX) {
                wasClicked = false;
            }

            float ratioInv = 1.0f * Math.Abs(attackCounter) / COUNTER_MAX; // 1 -> 0 -> 1
            float ratio = 1.0f - ratioInv; // 0 -> 1 -> 0

            Color c;
            SpriteRenderer sr;

            if (bkgd != null) {
                sr = bkgd.GetComponent<SpriteRenderer>();
                c = sr.color;
                c.b = ratioInv;
                c.g = ratioInv;
                sr.color = c;
            }

            if (bkgdOverlay != null) {
                sr = bkgdOverlay.GetComponent<SpriteRenderer>();
                c = sr.color;
                c.a = ratio;
                c.b = ratioInv;
                c.g = ratioInv;
                sr.color = c;
            }

            if (npc != null) {
                sr = npc.GetComponent<SpriteRenderer>();
                c = sr.color;
                c.a = ratioInv;
                c.b = ratioInv;
                c.g = ratioInv;
                sr.color = c;

                updateNpcScale(ratio);
                Transform t = npc.transform;
                Vector3 lea = t.localEulerAngles;
                lea.z = 45.0f * ratio;
                t.localEulerAngles = lea;
            }

            attackCounter--;
        }

        public bool approach() {
            if (npc == null || distance == Distance.Melee || distanceTransition) {
                return false;
            }

            moveDirection = -1;

            return true;
        }

        public bool escape() {
            if (npc == null || distance == Distance.None || distanceTransition) {
                return false;
            }

            moveDirection = 1;

            return true;
        }

        public void npcClick() {
            if (wasClicked || distance != Distance.Melee) {
                return;
            }

            wasClicked = true;
            attackCounter = 36;
        }

        public void updateNpcScale(float ratio = 0) {
            float scaleFactor = 0.1f * (4.0f + 4.0f * ratio - (int) app.distance -
                (float) moveDirection * moveCounter / COUNTER_MAX);
            npc.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
        }

    }

}