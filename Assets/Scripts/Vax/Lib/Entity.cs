using UnityEngine;

namespace Vax.Lib {

public class Entity<T> {
    public GameObject gameObject { get; protected set; }
    public T proto { get; protected set; }
}

}