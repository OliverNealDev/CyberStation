using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance;
    
    private NavMeshSurface _navMeshManager;

    private struct ColliderState
    {
        public Collider collider;
        public bool wasEnabled;
    }

    private struct RendererState
    {
        public Renderer renderer;
        public bool wasEnabled;
    }

    private void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        _navMeshManager = GetComponent<NavMeshSurface>();
        if (_navMeshManager == null)
        {
            Debug.LogError("NavMeshSurface component not found on this GameObject. Please add one.");
        }
        else
        {
            BuildNavMesh();
        }
    }
        
    public void BuildNavMesh()
    {
        if (_navMeshManager != null)
        {
            List<ColliderState> litterColliders = DisableLitterCollidersForBake();
            List<ColliderState> characterColliders = DisablePersonCollidersForBake();
            List<RendererState> characterRenderers = DisablePersonRenderersForBake();

            try
            {
                _navMeshManager.BuildNavMesh();
            }
            finally
            {
                RestoreRendererStates(characterRenderers);
                RestoreColliderStates(characterColliders);
                RestoreColliderStates(litterColliders);
            }
        }
        else
        {
            Debug.LogError("Cannot build NavMesh because NavMeshSurface component is missing.");
        }
    }

    private List<ColliderState> DisableLitterCollidersForBake()
    {
        List<ColliderState> colliderStates = new List<ColliderState>();
        Litter[] litterObjects = FindObjectsByType<Litter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Litter litter in litterObjects)
        {
            if (litter == null)
            {
                continue;
            }

            Collider[] colliders = litter.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                colliderStates.Add(new ColliderState
                {
                    collider = collider,
                    wasEnabled = collider.enabled
                });

                if (collider.enabled)
                {
                    collider.enabled = false;
                }
            }
        }

        return colliderStates;
    }

    private void RestoreColliderStates(List<ColliderState> colliderStates)
    {
        foreach (ColliderState colliderState in colliderStates)
        {
            if (colliderState.collider != null)
            {
                colliderState.collider.enabled = colliderState.wasEnabled;
            }
        }
    }

    private List<ColliderState> DisablePersonCollidersForBake()
    {
        List<ColliderState> colliderStates = new List<ColliderState>();
        Person[] people = FindObjectsByType<Person>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Person person in people)
        {
            if (person == null)
            {
                continue;
            }

            Collider[] colliders = person.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                colliderStates.Add(new ColliderState
                {
                    collider = collider,
                    wasEnabled = collider.enabled
                });

                if (collider.enabled)
                {
                    collider.enabled = false;
                }
            }
        }

        return colliderStates;
    }

    private List<RendererState> DisablePersonRenderersForBake()
    {
        List<RendererState> rendererStates = new List<RendererState>();
        Person[] people = FindObjectsByType<Person>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Person person in people)
        {
            if (person == null)
            {
                continue;
            }

            Renderer[] renderers = person.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                rendererStates.Add(new RendererState
                {
                    renderer = renderer,
                    wasEnabled = renderer.enabled
                });

                if (renderer.enabled)
                {
                    renderer.enabled = false;
                }
            }
        }

        return rendererStates;
    }

    private void RestoreRendererStates(List<RendererState> rendererStates)
    {
        foreach (RendererState rendererState in rendererStates)
        {
            if (rendererState.renderer != null)
            {
                rendererState.renderer.enabled = rendererState.wasEnabled;
            }
        }
    }
}
