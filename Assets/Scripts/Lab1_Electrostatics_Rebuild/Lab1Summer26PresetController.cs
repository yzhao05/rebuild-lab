using System.Collections;
using UnityEngine;

namespace EMVR.Lab1Rebuild
{
    public sealed class Lab1Summer26PresetController : MonoBehaviour
    {
        public Lab1Summer26SourceRuntime[] sources;

        private IEnumerator Start()
        {
            yield return null;
            ApplyPreset(1);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyPreset(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyPreset(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyPreset(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) ApplyPreset(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) ApplyPreset(5);
            if (Input.GetKeyDown(KeyCode.C)) ClearCharges();
        }

        public void ApplyPreset(int preset)
        {
            HideAll();

            if (preset == 1)
            {
                Activate(0, new Vector3(-1.2f, 0.3f, 0f), 2f);
                Activate(1, new Vector3(1.2f, 0.3f, 0f), -2f);
                Lab1RebuildStatus.Set("Summer26 preset 1: opposite point charges");
            }
            else if (preset == 2)
            {
                Activate(0, new Vector3(-1.2f, 0.3f, 0f), 2f);
                Activate(1, new Vector3(1.2f, 0.3f, 0f), 2f);
                Lab1RebuildStatus.Set("Summer26 preset 2: same-sign point charges");
            }
            else if (preset == 3)
            {
                Activate(2, new Vector3(-0.9f, 0.1f, 0f), 3f);
                Activate(0, new Vector3(1.25f, 0.45f, 0f), -2f);
                Lab1RebuildStatus.Set("Summer26 preset 3: line plus point charge");
            }
            else if (preset == 4)
            {
                Activate(4, new Vector3(-1.0f, 0f, 0f), 3f);
                Activate(5, new Vector3(1.0f, 0f, 0f), -3f);
                Lab1RebuildStatus.Set("Summer26 preset 4: opposite plane charges");
            }
            else
            {
                Activate(0, new Vector3(-1.45f, 0.65f, 0f), 2f);
                Activate(2, new Vector3(0f, -0.35f, 0f), -2f);
                Activate(4, new Vector3(1.45f, 0.45f, 0f), 1f);
                Lab1RebuildStatus.Set("Summer26 preset 5: mixed report challenge");
            }
        }

        public void ClearCharges()
        {
            if (sources == null) return;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null) sources[i].SetCharge(0f);
            }
            Lab1RebuildStatus.Set("Summer26 source charges cleared");
        }

        private void HideAll()
        {
            if (sources == null) return;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null) continue;
                sources[i].SetCharge(0f);
                sources[i].gameObject.SetActive(false);
            }
        }

        private void Activate(int index, Vector3 position, float charge)
        {
            if (sources == null || index < 0 || index >= sources.Length || sources[index] == null) return;
            sources[index].gameObject.SetActive(true);
            sources[index].transform.position = position;
            sources[index].SetCharge(charge);
        }
    }
}
