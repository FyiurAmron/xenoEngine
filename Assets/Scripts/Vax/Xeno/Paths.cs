namespace Vax.Xeno {

    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class Paths {

        public const string SFX_PATH = "Sfx/";
        public const string CONFIG_PATH = "Config/";

    }

    [Serializable]
    public class NpcProto {

        public string name;
        public string spriteName;
        public string bloodColor;

    }

    [Serializable]
    public class NpcConfig {

        public NpcProto[] npcProtos;

    }

    [Serializable]
    public class BkgdProto {

        public string name;
        public string spriteNearName;
        public string spriteFarName;
        public string fogColor;

    }

    [Serializable]
    public class BkgdConfig {

        public BkgdProto[] bkgdProtos;
        
        public Dictionary<string, BkgdProto> toBkgdMap () {
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

        public void playOneShot ( AudioSource audioSource = null ) {
            if ( audioSource == null ) {
                audioSource = App.app.audioSource;
            }
            audioSource.PlayOneShot( audioClip, volume );
        }

    }

    [Serializable]
    public class SfxConfig {

        public SfxProto[] sfxProtos;

        public Dictionary<string, SfxProto> toSfxMap () {
            var dictionary = new Dictionary<string, SfxProto>();
            foreach ( SfxProto v in sfxProtos ) {
                v.audioClip = Resources.Load<AudioClip>( Paths.SFX_PATH + v.clipName );
                dictionary[v.name] = v;
            }
            return dictionary;
        }

    }

}