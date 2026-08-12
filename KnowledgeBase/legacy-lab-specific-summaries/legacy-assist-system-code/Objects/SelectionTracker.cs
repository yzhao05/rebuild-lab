using EMVR.Assist.Data;
using UnityEngine;

namespace EMVR.Assist.Objects
{
    public class SelectionTracker : MonoBehaviour
    {
        public static SelectionTracker Instance { get; private set; }

        public ExperimentObject CurrentSelected { get; private set; }
        public ExperimentObject LastInteracted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetSelected(ExperimentObject obj)
        {
            if (CurrentSelected != null && CurrentSelected != obj)
            {
                CurrentSelected.SetSelected(false);
            }

            CurrentSelected = obj;

            if (CurrentSelected != null)
            {
                RememberInteracted(CurrentSelected);
                CurrentSelected.SetSelected(true);
            }
        }

        public void MarkInteracted(ExperimentObject obj)
        {
            RememberInteracted(obj);
        }

        public void ClearSelection()
        {
            if (CurrentSelected != null) CurrentSelected.SetSelected(false);
            CurrentSelected = null;
        }

        public ObjectSnapshot GetSelectedSnapshot()
        {
            ExperimentObject source = IsAvailable(CurrentSelected) ? CurrentSelected : null;
            if (source == null && IsAvailable(LastInteracted))
            {
                source = LastInteracted;
            }

            return source == null ? null : source.ToSnapshot();
        }

        private void RememberInteracted(ExperimentObject obj)
        {
            if (!IsAvailable(obj)) return;
            LastInteracted = obj;
        }

        private static bool IsAvailable(ExperimentObject obj)
        {
            return obj != null && obj.isActiveAndEnabled && obj.gameObject.activeInHierarchy;
        }
    }
}
