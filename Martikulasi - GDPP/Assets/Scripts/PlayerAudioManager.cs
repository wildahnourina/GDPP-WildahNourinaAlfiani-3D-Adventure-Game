using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField] AudioSource footstepSfx;
    [SerializeField] AudioSource landingSfx;
    [SerializeField] AudioSource glidingSfx;
    [SerializeField] AudioSource punchingSfx;

    private void PlayFootstepSfx()
    {
        footstepSfx.volume = Random.Range(.8f, 1f);
        footstepSfx.pitch = Random.Range(.8f, 1.5f);
        footstepSfx.Play();
    }

    private void PlayLandingSfx()
    {
        landingSfx.Play();
    }

    public void PlayGlidingSfx()
    {
        glidingSfx.Play();
    }

    public void StopGlidingSfx()
    {
        glidingSfx.Stop();
    }

    private void PlayPunchingSfx()
    {
        footstepSfx.volume = Random.Range(.8f, 1f);
        footstepSfx.pitch = Random.Range(.8f, 1.5f);
        punchingSfx.Play();
    }
}
