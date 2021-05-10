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

public class trackingMode : MonoBehaviour
{
    // Permet de sérialiser un composent même private.
    [SerializeField]
    private GameObject[] placeablePrefabs;

    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private ARTrackedImageManager trackedImageManager;

    // Pour la position de la camera
    private ARSessionOrigin arOrigin;

    // Text
    [SerializeField]
    private Text lockedImage;

    [SerializeField]
    private Text selectedDistance;

    private List<string> imagesName;

    // Instancie tous les objet et les rend invisible sinon ils sont tous affichés à l'origine de la caméra.
    // On récupère aussi les composant dont on a besoin (trackedImageManager et SessionOrigin)
    private void Awake()
    {
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        arOrigin = FindObjectOfType<ARSessionOrigin>();

        imagesName = new List<string>();

        foreach (GameObject prefab in placeablePrefabs)
        {
            GameObject newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            newPrefab.name = prefab.name;
            spawnedPrefabs.Add(prefab.name, newPrefab);
            spawnedPrefabs[prefab.name].SetActive(false);
            imagesName.Add(prefab.name);
        }

        lockedImage.text = "";
        selectedDistance.text = focusPosition.ToString();
    }

    // Binding
    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += ImageChanged;
        
    }

    // Unbind
    private void OnDisable()
    {
        foreach (GameObject prefab in spawnedPrefabs.Values)
        {
            prefab.SetActive(false);
        }
        trackedImageManager.trackedImagesChanged -= ImageChanged;
    }

    // EventHandler. Change les affichages et les positions des objets lorsque les images sont mises à jour.
    private void ImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            if(focus == true)
            {
                if(trackedImage.referenceImage.name == lockedImage.text)
                {
                    UpdateImage(trackedImage);
                }
                else
                {
                    spawnedPrefabs[trackedImage.referenceImage.name].SetActive(false);
                }
            }
            else
            {
                UpdateImage(trackedImage);
            }
        }
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            if (focus == true)
            {
                if (trackedImage.referenceImage.name == lockedImage.text)
                {
                    UpdateImage(trackedImage);
                }
                else
                {
                    spawnedPrefabs[trackedImage.referenceImage.name].SetActive(false);
                }
            }
            else
            {
                UpdateImage(trackedImage);
            }
        }
        // Théorique. Car les images ne sont jamais remove.
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            spawnedPrefabs[trackedImage.name].SetActive(false);
        }
    }

    // Met à jour la position des images, et affiche leur objet.
    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string name = trackedImage.referenceImage.name;
        Vector3 position = trackedImage.transform.position;
        Quaternion rotation = trackedImage.transform.rotation;

        GameObject prefab = spawnedPrefabs[name];
        prefab.transform.position = position;
        prefab.transform.rotation = rotation;
        prefab.SetActive(true);
    }






    // Pour garder le focus sur un objet
    private bool focus = false;

    public void enableFocus()
    {
        focus = true;
    }

    public void unableFocus()
    {
        focus = false;
    }





    // Entier permettant de choisir l'objet.
    private int imageIndex = 0;

    /*public void EnableSelectImage(bool enable)
    {
        selectImage = enabled;
        if (enable == true)
        {
            // On fait le focus sur l'objet que l'on souhaite détecter.
            focus = enable;
            lockedImage.text = imagesName[imageIndex];
        }
    }*/

    public void changeSelectedImage()
    {
        int imgNb = imagesName.Count;
        if (imageIndex < (imgNb - 1))
        {
            imageIndex += 1;
        }
        else
        {
            imageIndex = 0;
        }
        lockedImage.text = imagesName[imageIndex];
    }



    

    

    private void closestImage(ARTrackedImagesChangedEventArgs eventArgs)
    {
        countImage(eventArgs);
        Vector3 screenCenter = Camera.main.transform.position;
        float[] distances = new float[detectedImage];
        string[] names = new string[detectedImage];
        int placeInArray = 0;
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            Vector3 imagePosition = trackedImage.transform.position;
            distances[placeInArray] = Vector3.Distance(screenCenter, imagePosition);
            names[placeInArray] = trackedImage.referenceImage.name;
            placeInArray += 1;
        }
        if (detectedImage > 1)
        {
            // Rangement du tableau : on parcour le tableau et on va mettre les plus petites valeurs au début du tableau.
            int passage = 0;
            while (passage < (detectedImage - 1))
            {
                string imageToSwitch = "";
                float previousDistance = 0;
                for (int i = passage; i < detectedImage; i++)
                {
                    if (i == passage)
                    {
                        imageToSwitch = names[i];
                        previousDistance = distances[i];
                    }
                    else
                    {
                        if (distances[i] < previousDistance)
                        {
                            distances[passage] = distances[i];
                            names[passage] = names[i];
                            distances[i] = previousDistance;
                            names[i] = imageToSwitch;
                            previousDistance = distances[passage];
                            imageToSwitch = names[passage];
                        }
                    }
                }
                passage += 1;
            }
            lockedImage.text = names[focusPosition - 1];
        }
        else if (detectedImage == 1)
        {
            lockedImage.text = names[0];
        }
        else
        {
            lockedImage.text = "";
        }
    }





    // Nombre d'images repérées dans l'environnement
    private int detectedImage = 0;

    // EventHandler. Compte le nombre d'images reconnus et le met à jour à chaque fois que les images sont mises à jours.
    private void countImage(ARTrackedImagesChangedEventArgs eventArgs)
    {
        int count = 0;
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            count += 1;
        }
        if (count > detectedImage)
        {
            detectedImage = count;
        }
    }






    // Pour connaître la position de l'objet que l'on veut focus
    private int focusPosition = 1;
    // Permet de voir le numéro de l'image focus
    [SerializeField]
    private Text infoFocusedPosition;

    public void incPosition()
    {
        if (focusPosition < detectedImage)
        {
            focusPosition += 1;
            infoFocusedPosition.text = focusPosition.ToString();
        }
    }

    public void decPosition()
    {
        if (focusPosition > 1)
        {
            focusPosition -= 1;
            infoFocusedPosition.text = focusPosition.ToString();
        }
    }
}
