using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EMVR.Common.SceneFlow
{
    /// <summary>
    /// Shared scene-loading entry point for buttons and runtime triggers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrSceneLoader : MonoBehaviour
    {
        [Tooltip("Scene loaded by LoadConfiguredScene. The scene must be in Build Settings.")]
        public string configuredSceneName;

        public LoadSceneMode loadMode = LoadSceneMode.Single;
        public bool loadAsynchronously = true;
        public bool preventRepeatedRequests = true;

        bool isLoading;

        public event Action<string> SceneLoadStarted;
        public event Action<string> SceneLoadCompleted;

        public bool IsLoading
        {
            get { return isLoading; }
        }

        public void LoadConfiguredScene()
        {
            TryLoadConfiguredScene();
        }

        public bool TryLoadConfiguredScene()
        {
            return TryLoadScene(configuredSceneName);
        }

        public void LoadScene(string sceneName)
        {
            TryLoadScene(sceneName);
        }

        public bool TryLoadScene(string sceneName)
        {
            if (!CanStartLoad())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("EmVrSceneLoader requires a scene name.", this);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("Scene is not available in Build Settings: " + sceneName, this);
                return false;
            }

            return StartLoad(sceneName, delegate
            {
                return SceneManager.LoadSceneAsync(sceneName, loadMode);
            }, delegate
            {
                SceneManager.LoadScene(sceneName, loadMode);
            });
        }

        public void LoadSceneByBuildIndex(int buildIndex)
        {
            TryLoadSceneByBuildIndex(buildIndex);
        }

        public bool TryLoadSceneByBuildIndex(int buildIndex)
        {
            if (!CanStartLoad())
            {
                return false;
            }

            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError("Invalid scene build index: " + buildIndex, this);
                return false;
            }

            string label = "build-index:" + buildIndex;
            return StartLoad(label, delegate
            {
                return SceneManager.LoadSceneAsync(buildIndex, loadMode);
            }, delegate
            {
                SceneManager.LoadScene(buildIndex, loadMode);
            });
        }

        public void ReloadActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                TryLoadSceneByBuildIndex(activeScene.buildIndex);
            }
            else
            {
                TryLoadScene(activeScene.name);
            }
        }

        bool CanStartLoad()
        {
            return !preventRepeatedRequests || !isLoading;
        }

        bool StartLoad(
            string label,
            Func<AsyncOperation> asynchronousLoad,
            Action synchronousLoad)
        {
            isLoading = true;
            if (SceneLoadStarted != null)
            {
                SceneLoadStarted(label);
            }

            if (loadAsynchronously)
            {
                AsyncOperation operation = asynchronousLoad();
                if (operation == null)
                {
                    isLoading = false;
                    Debug.LogError("Unity could not start loading scene: " + label, this);
                    return false;
                }

                operation.completed += delegate
                {
                    CompleteLoad(label);
                };
                return true;
            }

            synchronousLoad();
            CompleteLoad(label);
            return true;
        }

        void CompleteLoad(string label)
        {
            isLoading = false;
            if (SceneLoadCompleted != null)
            {
                SceneLoadCompleted(label);
            }
        }
    }
}
