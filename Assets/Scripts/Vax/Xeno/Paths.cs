using Vax.Xeno.Entities;

namespace Vax.Xeno {

using System;
using System.Collections.Generic;
using UnityEngine;

public static class Paths {
    public const string SFX_PATH = "Sfx/";
    public const string CONFIG_PATH = "Config/";
    public const string GFX_PATH = "Gfx/";
    public const string OVERLAY_PATH = GFX_PATH + "Overlay/";
    public const string NPC_PATH = GFX_PATH + "Npc/";
    public const string BKGD_PATH = GFX_PATH + "Bkgd/";
}

[Serializable]
public class NpcConfig {
    public Dictionary<string, NpcProto> npcProtos;
}

[Serializable]
public class BkgdProto {
    public string name;
    public Dictionary<string, string> sprites;
    public string fogColor;
}

[Serializable]
public class BkgdConfig {
    public BkgdProto[] bkgdProtos;

    public Dictionary<string, BkgdProto> toBkgdMap() {
        var dictionary = new Dictionary<string, BkgdProto>();
        foreach ( BkgdProto v in bkgdProtos ) {
            //v.gameObject = ...;
            dictionary[v.name] = v;
        }

        return dictionary;
    }
}

[Serializable]
public class SfxProto {
    public string name;
    public string clipName;
    public float volume;

    public AudioClip audioClip = null;

    public void playOneShot( AudioSource audioSource = null ) {
        if ( audioSource == null ) {
            audioSource = App.app.audioSource;
        }

        audioSource.PlayOneShot( audioClip, volume );
    }
}

[Serializable]
public class SfxConfig {
    public SfxProto[] sfxProtos;

    public Dictionary<string, SfxProto> toSfxMap() {
        var dictionary = new Dictionary<string, SfxProto>();
        foreach ( SfxProto v in sfxProtos ) {
            v.audioClip = Utils.loadResource<AudioClip>( Paths.SFX_PATH + v.clipName );
            dictionary[v.name] = v;
        }

        return dictionary;
    }
}

}