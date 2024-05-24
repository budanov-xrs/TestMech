using System;

namespace Arion.Data.Utilities
{
    public class FpsCounter
    {
        private int _frameCount;
        private int _totalFrames;
        private DateTime _startTime;
        private DateTime _intervalStartTime;

        public void Start()
        {
            _frameCount = 0;
            _totalFrames = 0;
            _startTime = DateTime.Now;
            _intervalStartTime = DateTime.Now;
        }

        public void AddFrame()
        {
            _frameCount++;
        }

        public void CalculateAverageFPS(int seconds = 8)
        {
            TimeSpan intervalElapsedTime = DateTime.Now - _intervalStartTime;
            if (intervalElapsedTime.TotalSeconds >= seconds)
            {
                double averageFPS = _totalFrames / intervalElapsedTime.TotalSeconds;
                Console.WriteLine($"Average FPS for the last {seconds} seconds: {averageFPS:F1}");
                _totalFrames = 0;
                _intervalStartTime = DateTime.Now;
            }
        }

        public void Update()
        {
            TimeSpan elapsedTime = DateTime.Now - _startTime;
            if (elapsedTime.TotalSeconds >= 1)
            {
                //Console.WriteLine($"Frames per second: {_frameCount} ");
                _totalFrames += _frameCount;
                _frameCount = 0;
                _startTime = DateTime.Now;
            }
        }
    }
}
