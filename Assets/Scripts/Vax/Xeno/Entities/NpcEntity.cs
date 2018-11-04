using UnityEngine;

namespace Vax.Xeno.Entities {

using Lib;

public class NpcEntity : Entity<NpcProto> {
    public const float DEFAULT_Y_POS = -1.5f;
    
    public NpcEntity( string name, NpcConfig npcConfig ) {
        proto = npcConfig.npcProtos[name];

        gameObject = new GameObject( name );
        gameObject.AddComponent<BoxCollider2D>();
        gameObject.AddComponent<NpcClickHandler>();

        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = Utils.loadResource<Sprite>( "Npcs/" + name );

        spriteRenderer.sortingLayerName = "Npc";
        spriteRenderer.material = new Material( Utils.DEFAULT_SPRITE_SHADER );
        
        gameObject.setBoundsFromSprite();

        Vector3 pos = gameObject.transform.position;
        pos.y = DEFAULT_Y_POS;
        gameObject.transform.position = pos;
    }

    public void destroy() {
        Object.Destroy( gameObject );
    }
}

}