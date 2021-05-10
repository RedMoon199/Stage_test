using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Permet d'inclure automatiquement le composent.
[RequireComponent(typeof(ARTrackedImageManager))]
// Permet de récupérer la position de la camera
[RequireComponent(typeof(ARSessionOrigin))]

public class myImageTracking : MonoBehaviour
{
    // Permet de sérialiser un composent même private.
    [SerializeField]
    private GameObject[] placeablePrefabs;

    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private ARTrackedImageManager trackedImageManager;

    // Pour la position de la camera
    private ARSessionOrigin arOrigin;
    // Image la plus proche
    private String closestImage;

    // Text
    [SerializeField]
    private Text text;

    // Pour garder le focus sur un objet
    private bool focus = false;

    private void Awake()
    {
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        arOrigin = FindObjectOfType<ARSessionOrigin>();

        foreach (GameObject prefab in placeablePrefabs)
        {
            GameObject newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            newPrefab.name = prefab.name;
            spawnedPrefabs.Add(prefab.name, newPrefab);
            spawnedPrefabs[prefab.name].SetActive(false);
        }
    }

    // Binding
    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += ImageChanged;
    }

    // Unbind
    private void OnDisable()
    {
        foreach(GameObject prefab in spawnedPrefabs.Values)
        {
            prefab.SetActive(false);
        }
        trackedImageManager.trackedImagesChanged -= ImageChanged;
    }

    private void ImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Détermine l'image la plus proche (si on n'est pas en mode focus)
        if (focus == false)
        {
            whoIsTheClosest(eventArgs);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateImage(trackedImage);
        }
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateImage(trackedImage);
        }
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            spawnedPrefabs[trackedImage.name].SetActive(false);
            text.text = "Not tracked";
        }
    }

    private void whoIsTheClosest(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Permet de récupérer la position du centre de l'écran.
        Vector3 screenCenter = Camera.main.transform.position;
        float smallestDistance = 100000.0f;
        String nameReference = null;
        foreach(ARTrackedImage trackedImage in eventArgs.updated)
        {
            Vector3 imagePosition = trackedImage.transform.position;
            float distance = Vector3.Distance(screenCenter, imagePosition);
            if(distance < smallestDistance)
            {
                smallestDistance = distance;
                nameReference = trackedImage.referenceImage.name;
            }
        }

        closestImage = nameReference;
    }

    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string name = trackedImage.referenceImage.name;
        Vector3 position = trackedImage.transform.position;
        Quaternion rotation = trackedImage.transform.rotation;

        GameObject prefab = spawnedPrefabs[name];
        prefab.transform.position = position;
        prefab.transform.rotation = rotation;
        prefab.SetActive(true);

        // Text
        text.text = closestImage;

        foreach (GameObject go in spawnedPrefabs.Values)
        {
            if(go.name != closestImage)
            {
                go.SetActive(false);
            }
        }
    }

    public void EnableFocus(bool enable)
    {
        focus = enable;
    }
}
