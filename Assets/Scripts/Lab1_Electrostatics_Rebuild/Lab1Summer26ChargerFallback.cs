using UnityEngine;

namespace EMVR.Lab1Rebuild
{
    [RequireComponent(typeof(Collider))]
    public sealed class Lab1Summer26ChargerFallback : MonoBehaviour
    {
        public float sign = 1f;

        private void OnMouseDown()
        {
            Lab1Summer26SourceRuntime selected = Lab1Summer26SourceRuntime.Selected;
            if (selected == null)
            {
                Lab1RebuildStatus.Set("select a Summer26 charge object first");
                return;
            }

            selected.AddCharge(Mathf.Sign(sign));
            Lab1RebuildStatus.Set("charged " + selected.displayName + " to q = " + selected.GetCharge().ToString("0"));
        }
    }
}
