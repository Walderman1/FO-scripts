using UnityEngine;

public class audioBam : MonoBehaviour
{
    public AudioSource[] AS;
    public int Score;
    void Bam()
    {
        if (Score == 0)
        {
            AS[0].panStereo = 0;
            AS[0].pitch = 0.95f;
            AS[0].Play();
        }
        if (Score == 1)
        {
            AS[0].panStereo = -0.7f;
            AS[0].pitch = 1.05f;
            AS[0].Play();
        }
        if (Score == 2)
        {
            AS[0].panStereo = 0.7f;
            AS[0].pitch = 1f;
            AS[0].Play();
        }
        Score++;
    }
    void Back()
    {
        AS[1].Play();
    }
}
