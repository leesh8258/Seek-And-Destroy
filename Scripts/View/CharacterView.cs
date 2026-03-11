using UnityEngine;

public class CharacterView : MonoBehaviour
{
    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private Renderer[] bodyRenderers;

    public Transform WeaponAnchor => weaponAnchor;
    public Renderer[] BodyRenderers => bodyRenderers;
}
