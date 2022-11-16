using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Path : MonoBehaviour
{
    // Path settings
    private float meshWidth;
    private float meshSpacing;
    private float pointSpacing;
    private float pointResolution;

    // Guide materials
    private Material guideEnabledMaterial;

    // Path material
    private Material pathMaterial;

    // Path variables
    private (Vector3, Vector3, Vector3) pathPoints;
    private Vector3[] spacedPoints;
    private Vector3[] collisionPoints;

    // Collision holder
    private GameObject collisionHolder;

    // Collider variables
    private string colliderName;
    private bool setColliderTrigger;

    // Node holder
    private GameObject nodeHolder;
    public PathNode[] nodes;

    // Node variables
    private GameObject nodePrefab;
    private RuntimeAnimatorController nodeAnimatorController;

    public void UpdateVariables(PathBuilder pathBuilderScript, (Vector3, Vector3, Vector3) pathPoints)
    {
        // Set path settings
        meshWidth = pathBuilderScript.meshWidth;
        meshSpacing = pathBuilderScript.meshSpacing;
        pointSpacing = pathBuilderScript.pointSpacing;
        pointResolution = pathBuilderScript.pointResolution;

        // Get guide material from path builder script
        guideEnabledMaterial = pathBuilderScript.guideDefaultMaterial;

        // Get material from path builder script
        pathMaterial = pathBuilderScript.pathMaterial;

        // Get node variables
        nodePrefab = pathBuilderScript.nodePrefab;
        nodeAnimatorController = pathBuilderScript.nodeAnimatorController;

        // Set points
        this.pathPoints = pathPoints;
}

    public void InitializeMesh(bool isGuide, NodeController nodeController)
    {
        if (isGuide)
        {
            colliderName = PathBuilder.GuideNames.GuideCollider.ToString();
        }
        else
        {
            colliderName = PathBuilder.PathNames.PathCollider.ToString();
        }

        setColliderTrigger = isGuide;

        SetMesh();
        SetMaterialRendering(isGuide);
        CreateCollisions();
        if (!isGuide) HandleNodes(nodeController);
    }

    private void SetMesh()
    {
        SetPathSpacedPoints();

        // Add mesh components
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();

        // Set mesh
        gameObject.GetComponent<MeshFilter>().sharedMesh = PathUtilities.CreateMesh(spacedPoints, meshWidth);
    }

    public void UpdateMesh()
    {
        SetPathSpacedPoints();
        SetCollisionPoints();
        UpdateCollisions();

        // Set mesh
        gameObject.GetComponent<MeshFilter>().sharedMesh = PathUtilities.CreateMesh(spacedPoints, meshWidth);
    }

    private void SetMaterialRendering(bool isGuide)
    {
        // Change material tiling based on number of points
        float tiling = (-0.11f * meshSpacing) * (spacedPoints.Length / meshSpacing);

        // Update renderer
        Renderer renderer = gameObject.GetComponent<Renderer>();
        renderer.material = pathMaterial;
        renderer.material.mainTextureScale = new Vector2(1, tiling);
    }

    public void UpdateMaterial(Material material)
    {
        // Update renderer material
        gameObject.GetComponent<Renderer>().material = material;
    }

    private void SetCollisionPoints()
    {
        List<Vector3> collisionPointsList = new List<Vector3>();
        for (int i = 0; i < spacedPoints.Length; i++)
        {
            // Every 5 points, add collision
            if ((i % 5) == 0)
            {
                collisionPointsList.Add(spacedPoints[i]);
            }
        }

        collisionPointsList.Add(spacedPoints[spacedPoints.Length - 1]);

        collisionPoints = collisionPointsList.ToArray();
    }

    private void CreateCollisions()
    {
        SetPathSpacedPoints();
        SetCollisionPoints();

        // Create collision holder object
        collisionHolder = new GameObject(PathBuilder.PathNames.Collisions.ToString());
        collisionHolder.transform.SetParent(gameObject.transform);

        // Create collision objects
        for (int i = 0; i < collisionPoints.Length; i++)
        {
            PathUtilities.CreateCollider(colliderName, collisionHolder.transform, collisionPoints[i], setColliderTrigger);
        }
    }

    private void UpdateCollisions()
    {
        Transform collisionsTransform = gameObject.transform.Find(PathBuilder.PathNames.Collisions.ToString());

        // Add colliders to match points
        if (collisionPoints.Length > collisionsTransform.childCount)
        {
            // Transform existing colliders
            for (int i = 0; i < collisionsTransform.childCount; i++)
            {
                collisionsTransform.transform.GetChild(i).transform.position = collisionPoints[i];
            }

            // Create new colliders
            for (int i = collisionsTransform.childCount; i < collisionPoints.Length; i++)
            {
                PathUtilities.CreateCollider(colliderName, collisionHolder.transform, collisionPoints[i], setColliderTrigger);
            }
        }
        // Destroy colliders to match points
        else if (collisionPoints.Length < collisionsTransform.childCount)
        {
            // Transform existing colliders
            for (int i = 0; i < collisionPoints.Length; i++)
            {
                collisionsTransform.transform.GetChild(i).transform.position = collisionPoints[i];
            }

            // Destroy excess colliders
            for (int i = collisionPoints.Length; i < collisionsTransform.childCount; i++)
            {
                Destroy(collisionsTransform.GetChild(i).gameObject);
            }
        }
        // Set new position of existing colliders
        else
        {
            for (int i = 0; i < collisionsTransform.childCount; i++)
            {
                collisionsTransform.transform.GetChild(i).transform.position = collisionPoints[i];
            }
        }
    }

    private void HandleNodes(NodeController nodeController)
    {
        // Create node holder object
        nodeHolder = new GameObject(PathBuilder.NodeNames.NodeHolder.ToString());
        nodeHolder.transform.SetParent(gameObject.transform);

        nodes = new PathNode[2];

        PathNode existingNode;

        // Set nodes
        for (int i = 0; i < 2; i++)
        {
            Vector3 nodePoint;
            if (i == 0)
            {
                nodePoint = pathPoints.Item1;
                
            }
            else
            {
                nodePoint = pathPoints.Item2;
            }

            existingNode = nodeController.CheckExistingNode(nodePoint);

            GameObject node;
            if (existingNode != null)
            {
                node = existingNode.gameObject;
            }
            else
            {
                node = Instantiate(nodePrefab, nodePoint, Quaternion.identity);
                node.name = PathBuilder.NodeNames.Node.ToString();
                node.transform.SetParent(nodeHolder.transform);

                // Add node component
                node.AddComponent<PathNode>();
                node.GetComponent<PathNode>().InitializeAnimator(nodeAnimatorController);
            }

            nodes[i] = node.GetComponent<PathNode>();
        }
    }

    private void SetPathSpacedPoints()
    {
        if (pathPoints.Item3 != Vector3.zero)
        {
            spacedPoints = PathUtilities.CalculateSpacedPoints(pathPoints, true, pointSpacing, pointResolution);
        }
        else
        {
            spacedPoints = PathUtilities.CalculateSpacedPoints(pathPoints, false, pointSpacing, pointResolution);
        }
    }
}
