using UnityEngine;

public class WeaponView : MonoBehaviour
{
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private Renderer[] weaponRenderers;

    public Transform Muzzle => muzzle;
    public Transform FireOrigin => fireOrigin;
    public Renderer[] WeaponRenderers => weaponRenderers;
}
