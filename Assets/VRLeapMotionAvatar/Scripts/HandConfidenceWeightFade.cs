using UnityEngine;
using Leap.Unity;

namespace CpvrLab.VirtualTable
{
    /// <summary>
    /// Fade a hand pose between a leap hand model and a static hand model based
    /// on the confidence value of the leap hand.
    /// </summary>
    [RequireComponent(typeof(HandPoseLerp))]
    public class HandConfidenceWeightFade : MonoBehaviour
    {
        // animation curve managing hand tracking fade in interpolation
        public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        // fade to the relaxed hand state or fade out the ik goals completely
        public bool useRelaxHandGoals = true;

        // Should the hand confidence fall below this threshold then we'll fade out
        [Range(0, 1)]
        public float confidenceThreshold = 0.12f;

        public float fadeDuration = 0.5f;        
        public HandModel handModel;
        private HandPoseLerp _handLerper;
        
        private float _timer = 0.0f;
        private float _timeFactor = 1.0f;
        private bool _fadeRunning;
        private FadeDir _fadeDir;
        private float _startWeight = 0.0f;
        private long _prevFrameId = -1;
        private float _currentFrameAge = 0.0f;
        private float _cachedConfidence = 0.0f;
        // duration in second of not receiving a new frame from the leap hand
        // after which we assume a confidence of 0 
        private float _leapTimeOutThreshold = 0.1f; 

        private enum FadeDir
        {
            In = 1,
            Out = 0
        }

        protected virtual void Awake()
        {
            _handLerper = GetComponent<HandPoseLerp>();
            if (_handLerper == null)
                Debug.LogError("HandConfidenceWeightFade: Couldn't find a HandPoseLerp component attached!");
        }
        
        void UpdateCachedConfidence()
        {
            var leapHand = handModel.GetLeapHand();
            
            if (leapHand == null)
            {
                _cachedConfidence = 0.0f;
                return;
            }
            
            // update or reset the frame age variable
            if (_prevFrameId != leapHand.FrameId)
                _currentFrameAge = 0.0f;
            else            
                _currentFrameAge += Time.deltaTime;
            
            // if our prev frame's age is over our set threshold then we
            // assume we lost tracking and therefore assume a confidence of 0
            if (_currentFrameAge > _leapTimeOutThreshold)
            {
                _cachedConfidence = 0.0f;
                return;
            }

            // remember previous frame id
            _prevFrameId = leapHand.FrameId;

            // set real confidence
            _cachedConfidence = handModel.GetLeapHand().Confidence;
        }

        void Fade(FadeDir dir)
        {
            // don't do anything if we're already fading in the same direction
            // or are already faded in completely in that direction
            if (_fadeDir == dir)
                return;

            // start a new fade
            _timer = 0.0f;
            _timeFactor = 1.0f / fadeDuration; // we could calculate this only at the start but what if the user changes the duration
            _startWeight = _handLerper.alpha;
            _fadeDir = dir;
            _fadeRunning = true;
        }


        void Update()
        {
            UpdateCachedConfidence();

            if (_cachedConfidence < confidenceThreshold)            
                Fade(FadeDir.Out);            
            else            
                Fade(FadeDir.In);            

            if (_fadeRunning)
                DoFade();
        }

        void DoFade()
        {
            _timer += Time.deltaTime;
            float normalizedTime = _timer * _timeFactor;
            float value = fadeInCurve.Evaluate(normalizedTime);

            _handLerper.alpha = Mathf.Lerp(_startWeight, (float)_fadeDir, value);

            if (_timer >= fadeDuration)            
                _fadeRunning = false;            
        }        
    }    

}
