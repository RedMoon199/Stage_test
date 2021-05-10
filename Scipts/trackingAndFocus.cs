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

public class trackingAndFocus : MonoBehaviour
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
    private Text imageSee;

    
    private List<string> imagesName;


    // Instancie tous les objet et les rend invisible sinon ils sont tous affichés à l'origine de la caméra.
    // On récupère aussi les composant dont on a besoin (trackedImageManager et SessionOrigin)
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

        imagesName = new List<string>();
        foreach(GameObject image in spawnedPrefabs.Values)
        {
            imagesName.Add(image.name);
        }

        numImageFocused.text = focusPosition.ToString();
    }

    // Binding
    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += ImageCountUpdate;
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
        trackedImageManager.trackedImagesChanged -= ImageCountUpdate;
    }



    // Pour garder le focus sur un objet
    private bool focus = false;

    public void EnableFocus(bool enable)
    {
        focus = enable;
    }

    // Pour sélectionner une image à tracker
    private bool selectImage = false;
    // Entier permettant de choisir l'objet.
    private int imageIndex = 0;

    public void EnableSelectImage(bool enable)
    {
        selectImage = enabled;
        if(enable == true)
        {
            // On fait le focus sur l'objet que l'on souhaite détecter.
            focus = enable;
            focusedImage = imagesName[imageIndex];
        }
    }

    public void changeSelectedImage()
    {
        int imgNb = imagesName.Count;
        if(imageIndex < (imgNb - 1))
        {
            imageIndex += 1;
        }
        else
        {
            imageIndex = 0;
        }
        focusedImage = imagesName[imageIndex];
    }



    // EventHandler. Change les affichages et les positions des objets lorsque les images sont mises à jour.
    private void ImageChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Détermine l'image la plus proche (si on n'est pas en mode focus)
        if (focus == false)
        {
            selectFocus(eventArgs);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateImage(trackedImage);
        }
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateImage(trackedImage);
        }
        // Théorique. Car les images ne sont jamais remove.
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            spawnedPrefabs[trackedImage.name].SetActive(false);
            imageSee.text = "Not tracked";
        }
    }

    // Nombre d'images repérées dans l'environnement
    private int imageDetected = 0;

    /*[SerializeField]
    private Text detectedImagesNumber;*/

    // EventHandler. Compte le nombre d'images reconnus et le met à jour à chaque fois que les images sont mises à jours.
    private void ImageCountUpdate(ARTrackedImagesChangedEventArgs eventArgs)
    {
        int count = 0;
        foreach(ARTrackedImage trackedImage in eventArgs.updated)
        {
            count += 1;
        }
        if(count > imageDetected)
        {
            imageDetected = count;
        }
       /* detectedImagesNumber.text = "nbImage : " + imageDetected.ToString();
        //Pas approprié mais bon...
        focusedImage.text = "focusImg : " + focusPosition.ToString();*/
    }

    // Pour connaître la position de l'objet que l'on veut focus
    private int focusPosition = 1;
    // Permet de voir le numéro de l'image focus
    [SerializeField]
    private Text numImageFocused; 


    public void incPosition()
    {
        if (focusPosition < imageDetected)
        {
            focusPosition += 1;
            numImageFocused.text = focusPosition.ToString();
        }
    }

    public void decPosition()
    {
        if (focusPosition > 1)
        {
            focusPosition -= 1;
            numImageFocused.text = focusPosition.ToString();
        }
    }

    // Image sur laquelle on a le focus
    private String focusedImage;

    private void selectFocus(ARTrackedImagesChangedEventArgs eventArgs)
    {
        Vector3 screenCenter = Camera.main.transform.position;
        float[] distances = new float[imageDetected];
        string[] referenceNames = new string[imageDetected];
        int placeInArray = 0;
        foreach(ARTrackedImage trackedImage in eventArgs.updated)
        {
            Vector3 imagePosition = trackedImage.transform.position;
            distances[placeInArray] = Vector3.Distance(screenCenter, imagePosition);
            referenceNames[placeInArray] = trackedImage.referenceImage.name;
            placeInArray += 1;
        }
        if(imageDetected > 1)
        {
            // Rangement du tableau : on parcour le tableau et on va mettre les plus petites valeurs au début du tableau.
            int passage = 0;
            while(passage < (imageDetected - 1))
            {
                string imageToSwitch = "";
                float previousDistance = 0;
                for(int i = passage; i < imageDetected; i++)
                {
                    if(i == passage)
                    {
                        imageToSwitch = referenceNames[i];
                        previousDistance = distances[i];
                    }
                    else
                    {
                        if(distances[i] < previousDistance)
                        {
                            distances[passage] = distances[i];
                            referenceNames[passage] = referenceNames[i];
                            distances[i] = previousDistance;
                            referenceNames[i] = imageToSwitch;
                            previousDistance = distances[passage];
                            imageToSwitch = referenceNames[passage];
                        }
                    }
                }
                passage += 1;
            }
            focusedImage = referenceNames[focusPosition - 1];
        }
        if(imageDetected == 1)
        {
            focusedImage = referenceNames[0];
        }
        // Si on détecte 0 images on ne fait rien
    }

    // Met à jour la position des images, et affiche seulement l'image sur laquelle on a le focus.
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
        imageSee.text = focusedImage;

        foreach (GameObject go in spawnedPrefabs.Values)
        {
            if (go.name != focusedImage)
            {
                go.SetActive(false);
            }
        }
    }
}
