using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Added namespace to avoid conflicts
namespace SimpleWaterShader
{
    public class WaterReflection : MonoBehaviour
    {
        // references
        Camera mainCamera;
        Camera reflectionCamera;
        
        [Tooltip("The plane where the camera will be reflected, the water plane or any object with the same position and rotation")]
        public Transform reflectionPlane;
        
        [Tooltip("The texture used by the Water shader to display the reflection")]
        public RenderTexture outputTexture;
        
        // parameters
        public bool copyCameraParameters; // Fixed typo: was "copyCameraParamerers"
        public float verticalOffset;
        
        private bool isReady;
        
        // cache
        private Transform mainCamTransform;
        private Transform reflectionCamTransform;
        
        private void Awake() // Changed from public to private
        {
            mainCamera = Camera.main;
            reflectionCamera = GetComponent<Camera>();
            Validate();
        }
        
        private void Update()
        {
            if (isReady)
                RenderReflection();
        }
        
        private void RenderReflection()
        {
            // take main camera directions and position world space
            Vector3 cameraDirectionWorldSpace = mainCamTransform.forward;
            Vector3 cameraUpWorldSpace = mainCamTransform.up;
            Vector3 cameraPositionWorldSpace = mainCamTransform.position;
            cameraPositionWorldSpace.y += verticalOffset;
            
            // transform direction and position by reflection plane
            Vector3 cameraDirectionPlaneSpace = reflectionPlane.InverseTransformDirection(cameraDirectionWorldSpace);
            Vector3 cameraUpPlaneSpace = reflectionPlane.InverseTransformDirection(cameraUpWorldSpace);
            Vector3 cameraPositionPlaneSpace = reflectionPlane.InverseTransformPoint(cameraPositionWorldSpace);
            
            // invert direction and position by reflection plane
            cameraDirectionPlaneSpace.y *= -1;
            cameraUpPlaneSpace.y *= -1;
            cameraPositionPlaneSpace.y *= -1;
            
            // transform direction and position from reflection plane local space to world space
            cameraDirectionWorldSpace = reflectionPlane.TransformDirection(cameraDirectionPlaneSpace);
            cameraUpWorldSpace = reflectionPlane.TransformDirection(cameraUpPlaneSpace);
            cameraPositionWorldSpace = reflectionPlane.TransformPoint(cameraPositionPlaneSpace);
            
            // apply direction and position to reflection camera
            reflectionCamTransform.position = cameraPositionWorldSpace;
            reflectionCamTransform.LookAt(cameraPositionWorldSpace + cameraDirectionWorldSpace, cameraUpWorldSpace);
        }
        
        private void Validate()
        {
            // Fixed validation logic - check both cameras before setting isReady
            bool hasMainCamera = false;
            bool hasReflectionCamera = false;
            
            if (mainCamera != null)
            {
                mainCamTransform = mainCamera.transform;
                hasMainCamera = true;
            }
            
            if (reflectionCamera != null)
            {
                reflectionCamTransform = reflectionCamera.transform;
                hasReflectionCamera = true;
            }
            
            // Only set isReady to true if both cameras are valid
            isReady = hasMainCamera && hasReflectionCamera && reflectionPlane != null;
            
            // Copy camera parameters if requested and everything is ready
            if (isReady && copyCameraParameters)
            {
                copyCameraParameters = false; // Reset the flag
                reflectionCamera.CopyFrom(mainCamera);
                reflectionCamera.targetTexture = outputTexture;
            }
        }
    }
}