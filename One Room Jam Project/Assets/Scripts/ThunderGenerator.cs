using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThunderGenerator : MonoBehaviour
{

    private AudioSource audioSource;
    private List<AudioClip> playedClips = new List<AudioClip>();
    private float waitTime;

    public List<AudioClip> thunderSounds = new List<AudioClip>();



    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();

        waitTime = Time.time + Random.Range(5f, 35f);
    }


    private void Update()
    {
        if (Time.time > waitTime)
        {
            Thunder();
        }
    }


    private void Thunder()
    {

        audioSource.transform.position = new Vector3(Random.Range(-30f, 30f), 0f, Random.Range(-30f, 30f));

        int pickNum = Random.Range(0, thunderSounds.Count);

        audioSource.pitch = 1f + (Random.Range(-0.2f, 0.2f));
        AudioSource clip = PlayClipAt(thunderSounds[pickNum], audioSource.transform.position);
        clip.pitch = audioSource.pitch;
        clip.spatialBlend = 1.0f;
        clip.minDistance = audioSource.minDistance;
        clip.maxDistance = audioSource.maxDistance;
        //audioSource.PlayOneShot(thunderSounds[pickNum]);


        playedClips.Add(thunderSounds[pickNum]);
        thunderSounds.RemoveAt(pickNum);

        if (thunderSounds.Count <= 0)
        {
            thunderSounds.AddRange(playedClips);
            playedClips.Clear();
        }

        waitTime = Time.time + Random.Range(5f, 35f);

    }


    AudioSource PlayClipAt(AudioClip clip, Vector3 pos)
    {
        GameObject tempGO = new GameObject("TempAudio"); // create the temp object
        tempGO.transform.position = pos; // set its position
        AudioSource aSource = tempGO.AddComponent<AudioSource>(); // add an audio source
        aSource.clip = clip; // define the clip
                             // set other aSource properties here, if desired
        aSource.Play(); // start the sound
        Destroy(tempGO, clip.length); // destroy object after clip duration
        return aSource; // return the AudioSource reference
    }


}
