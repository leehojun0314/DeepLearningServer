using System.Diagnostics;

namespace DeepLearningServer.Classes
{
    public class MeasureExecutionTime
    {
        /// <summary>
        /// 주어진 액션(함수)의 실행 시간을 측정합니다.
        /// </summary>
        /// <param name="action">측정할 함수</param>
        /// <returns>실행 시간 (TimeSpan)</returns>
        public static TimeSpan Measure(Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew(); // 타이머 시작
            action();  // 함수 실행
            stopwatch.Stop();  // 타이머 정지

            return stopwatch.Elapsed;  // 실행 시간 반환
        }
    }
}
