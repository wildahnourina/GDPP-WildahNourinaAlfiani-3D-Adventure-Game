using System;
using Unity.Cinemachine;
using UnityEngine;


public enum CameraState { ThirdPerson, FirstPerson }

public class CameraManager : MonoBehaviour
{
    public Action OnChangePerspective;

    [SerializeField] InputManager input;
    public CameraState cameraState;

    [SerializeField] private CinemachineCamera fpsCamera;
    [SerializeField] private CinemachineCamera tpsCamera;

    private void Start()
    {
        input.OnChangePOV += SwitchCamera;
    }

    private void OnDestroy()
    {
        input.OnChangePOV -= SwitchCamera;
    }

    public void SetTPSFieldOfView(float fieldOfView) => tpsCamera.Lens.FieldOfView = fieldOfView;

    public void SetFPSClampedCamera(bool isClamped, Vector3 playerRotation)
    {
        CinemachinePanTilt pov = fpsCamera.GetComponent<CinemachinePanTilt>();
        if (isClamped)
        {
            pov.PanAxis.Wrap = false;
            pov.PanAxis.Range.x = playerRotation.y - 45;
            pov.PanAxis.Range.y = playerRotation.y + 45;
        }
        else
        {
            pov.PanAxis.Range.x = -180;
            pov.PanAxis.Range.y = 180;
            pov.PanAxis.Wrap = true;
        }
    }

    private void SwitchCamera()
    {
        OnChangePerspective();

        if (cameraState == CameraState.ThirdPerson)
        {
            cameraState = CameraState.FirstPerson;
            tpsCamera.gameObject.SetActive(false);
            fpsCamera.gameObject.SetActive(true);
        }
        else
        {
            cameraState = CameraState.ThirdPerson;
            tpsCamera.gameObject.SetActive(true);
            fpsCamera.gameObject.SetActive(false);
        }
    }
}
