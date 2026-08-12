using UnityEngine;

namespace EMVR.Common.Contracts
{
    /// <summary>
    /// Separates reusable interaction from lab-specific placement rules.
    /// </summary>
    public interface IPlacementCondition
    {
        bool IsSatisfied(Transform target);
    }
}
