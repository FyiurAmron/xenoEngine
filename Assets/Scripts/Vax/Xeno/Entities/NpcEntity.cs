using System.IO;
using UnityEngine;

namespace Vax.Xeno.Entities {

using Lib;

public class NpcEntity : Entity<NpcProto> {
    public NpcEntity( string name, NpcConfig npcConfig ) {
        proto = npcConfig.npcProtos[name];

        gameObject = new GameObject( name );
        gameObject.AddComponent<BoxCollider2D>();
        gameObject.AddComponent<NpcClickHandler>();

        SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = Resources.Load<Sprite>( "Npcs/" + name );
        if ( sr.sprite == null ) {
            throw new FileNotFoundException();
        }

        sr.sortingLayerName = "Npc";

        gameObject.setBoundsFromSprite();

        Vector2 pos = gameObject.transform.position;
        pos.y = -1.0f;
        gameObject.transform.position = pos;
    }

    public void destroy() {
        Object.Destroy( gameObject );
    }
}

}