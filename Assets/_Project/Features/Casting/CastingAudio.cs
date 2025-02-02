using BacklineVR.Casting;
using UnityEngine;

namespace BacklineVR.Casting
{
    public class CastingAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource[] _audioSources;
        [SerializeField] private Vector2 _brushAudioPitchVelocityRange;
        [SerializeField] private AudioSource _castingSound;
        private float _audioVolumeDesired;
        private float _currentTotalVolume = 0; // Brush audio volume before being divided between layers
        private float _brushAudioMaxVolume = 1;
        private float _brushAudioAdjustSpeedUp = .5f;
        private float _brushAudioAdjustSpeedDown = 1.4f;
        private Vector2 _brushAudioVolumeVelocityRange = new Vector2(.5f, 10 * 2.5f);
        private float _brushAudioBasePitch = 1;
        private float _brushAudioMaxPitchShift = .03f;
        private float _audioPitchDesired = 1;
        private Vector3 _previousPosition; //used for audio
        private bool _playing = false;
        private void Awake()
        {
            var input = GetComponent<GrabCaster>();
            input.OnStrokeStart += StartStrokeAudio;
            input.OnStrokeEnd += SilenceAudio;
            input.OnCast += PlayCast;
        }
        private void Update()
        {
            if (!_playing)
                return;

            float fMovementSpeed = Vector3.Distance(_previousPosition, transform.position) /
                Time.deltaTime;

            float fVelRangeRange = _brushAudioVolumeVelocityRange.y - _brushAudioVolumeVelocityRange.x;
            float fVolumeRatio = Mathf.Clamp01((fMovementSpeed - _brushAudioVolumeVelocityRange.x) / fVelRangeRange);
            _audioVolumeDesired = fVolumeRatio;

            float fPitchRangeRange = _brushAudioPitchVelocityRange.y - _brushAudioPitchVelocityRange.x;
            float fPitchRatio = Mathf.Clamp01((fMovementSpeed - _brushAudioPitchVelocityRange.x) / fPitchRangeRange);
            _audioPitchDesired = _brushAudioBasePitch + (fPitchRatio * _brushAudioMaxPitchShift);

            //smooth volume and pitch out a bit from frame to frame
            float fFadeStepUp = _brushAudioAdjustSpeedUp * Time.deltaTime;
            float fFadeStepDown = _brushAudioAdjustSpeedDown * Time.deltaTime;

            float fVolumeDistToDesired = _audioVolumeDesired - _currentTotalVolume;
            float fVolumeAdjust = Mathf.Clamp(fVolumeDistToDesired, -fFadeStepDown, fFadeStepUp);
            _currentTotalVolume = _currentTotalVolume + fVolumeAdjust;
            float fPitchDistToDesired = _audioPitchDesired - _audioSources[0].pitch;
            float fPitchAdjust = Mathf.Clamp(fPitchDistToDesired, -fFadeStepDown, fFadeStepUp);

            for (int i = 0; i < _audioSources.Length; i++)
            {
                // Adjust volume of each layer based on brush speed
                _audioSources[i].volume = LayerVolume(i, _currentTotalVolume);
                _audioSources[i].pitch += fPitchAdjust;
            }
            _previousPosition = transform.position;
        }
        private void StartStrokeAudio()
        {
            for (int i = 0; i < _audioSources.Length; i++)
            {
                _audioSources[i].volume = 0;
                _audioSources[i].Play();
            }
            _playing = true;
        }
        // Defines volume of a specific layer, given the total volume of the brush;
        private float LayerVolume(int iLayer, float fTotalVolume)
        {
            float fLayerBeginning;
            if (iLayer == 0)
            {
                fLayerBeginning = 0f;
            }
            else if (iLayer == 1)
            {
                fLayerBeginning = 1f / 3f;
            }
            else if (iLayer == 2)
            {
                fLayerBeginning = .5f;
            }
            else if (iLayer == 3)
            {
                fLayerBeginning = 2f / 3f;
            }
            else
            {
                fLayerBeginning = 5f / 6f;
            }
            float fLayerLength = 1f - fLayerBeginning;
            float fLayerVolume = (fTotalVolume - fLayerBeginning) / fLayerLength;
            fLayerVolume *= _brushAudioMaxVolume;
            var fResult = Mathf.Clamp01(fLayerVolume);
            return fResult;
        }
        private void SilenceAudio()
        {
            _audioVolumeDesired = 0.0f;
            _audioPitchDesired = 1.0f;
            for (int i = 0; i < _audioSources.Length; i++)
            {
                _audioSources[i].volume = 0;
                _audioSources[i].Stop();
            }
            _playing = false;
        }
        private void PlayCast()
        {
            _castingSound.Play();
        }
    }
} // namespace TiltBrush
