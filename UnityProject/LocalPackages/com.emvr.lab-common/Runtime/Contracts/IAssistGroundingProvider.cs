namespace EMVR.Common.Contracts
{
    /// <summary>
    /// Builds the grounded JSON payload consumed by a lab-assist gateway.
    /// </summary>
    public interface IAssistGroundingProvider
    {
        string BuildAssistGroundingJson();
    }
}
