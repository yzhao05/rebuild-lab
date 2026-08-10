using System;
using UnityEngine;

namespace EMVR.Common.Objects
{
    /// <summary>
    /// A reusable lab object with one bounded numeric domain parameter. It is
    /// deliberately neutral: consuming labs can name the value voltage,
    /// distance, charge, mass, or another experiment-specific quantity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrParameterizedLabObject : EmVrLabObject
    {
        [SerializeField] string parameterName = "value";
        [SerializeField] float parameterValue;
        [SerializeField] float minimumValue;
        [SerializeField] float maximumValue = 10f;

        public string ParameterName
        {
            get { return parameterName; }
        }

        public float ParameterValue
        {
            get { return parameterValue; }
        }

        public void ConfigureParameter(string newName, float newValue, float minimum, float maximum)
        {
            parameterName = string.IsNullOrWhiteSpace(newName) ? "value" : newName.Trim();
            minimumValue = Mathf.Min(minimum, maximum);
            maximumValue = Mathf.Max(minimum, maximum);
            parameterValue = Mathf.Clamp(newValue, minimumValue, maximumValue);
        }

        public void SetParameterValue(float value)
        {
            parameterValue = Mathf.Clamp(value, minimumValue, maximumValue);
        }

        public override string ExportStateJson()
        {
            ParameterizedState state = new ParameterizedState
            {
                transform_state_json = base.ExportStateJson(),
                parameter_name = ParameterName,
                parameter_value = ParameterValue
            };
            return JsonUtility.ToJson(state);
        }

        public override bool TryImportStateJson(string stateJson, out string error)
        {
            if (string.IsNullOrWhiteSpace(stateJson))
            {
                error = "Parameterized state JSON is empty.";
                return false;
            }

            ParameterizedState state;
            try
            {
                state = JsonUtility.FromJson<ParameterizedState>(stateJson);
            }
            catch (Exception exception)
            {
                error = "Parameterized state JSON is invalid: " + exception.Message;
                return false;
            }

            if (state == null || string.IsNullOrWhiteSpace(state.transform_state_json))
            {
                error = "Parameterized state is missing its transform state.";
                return false;
            }

            if (!string.Equals(state.parameter_name, ParameterName, StringComparison.Ordinal))
            {
                error = "State parameter '" + state.parameter_name
                    + "' does not match target parameter '" + ParameterName + "'.";
                return false;
            }

            if (!base.TryImportStateJson(state.transform_state_json, out error))
            {
                return false;
            }

            SetParameterValue(state.parameter_value);
            return true;
        }

        [Serializable]
        sealed class ParameterizedState
        {
            public string transform_state_json;
            public string parameter_name;
            public float parameter_value;
        }
    }
}
