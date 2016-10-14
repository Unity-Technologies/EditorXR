using System;
using System.Collections;
using UnityEngine.Events;

namespace UnityEngine.Experimental.Tweening
{
	// Base interface for tweeners, 
	// using an interface instead of 
	// an abstract class as we want the
	// tweens to be structs.
	public interface ITweenValue
	{
		void TweenValue(float floatPercentage);
		bool ignoreTimeScale { get; }
		float duration { get; }
		bool ValidOnChangedTarget();
	}

	// Example usage: move an object from (0, 0, 0) to (1, 1, 1) over 1s
	//	void Start()
	//	{
	//		PositionTween positionTween = new PositionTween
	//		{
	//			startPosition = Vector3.zero,
	//			targetPosition = Vector3.one,
	//			duration = 1f
	//		};
	//	
	//		positionTween.AddOnChangedCallback(MoveObject);
	//	
	//	 	var tweenRunner = new TweenRunner<PositionTween>();
	//		tweenRunner.Init(this);
	//		tweenRunner.StartTween(positionTween);
	//	}
	//	
	//	public void MoveObject(Vector3 position)
	//	{
	//		transform.position = position;
	//	}
	internal struct PositionTween : ITweenValue
	{
		public class PositionTweenCallback : UnityEvent<Vector3>
		{
		}

		private PositionTweenCallback m_OnChangedTarget;
		private UnityEvent m_OnCompleteTarget;
		private Vector3 m_StartPosition;
		private Vector3 m_TargetPosition;

		private float m_Duration;
		private bool m_IgnoreTimeScale;

		private AnimationCurve m_CustomAnimationCurve;

		public Vector3 startPosition
		{
			get { return m_StartPosition; }
			set { m_StartPosition = value; }
		}

		public Vector3 targetPosition
		{
			get { return m_TargetPosition; }
			set { m_TargetPosition = value; }
		}

		public float duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public bool ignoreTimeScale
		{
			get { return m_IgnoreTimeScale; }
			set { m_IgnoreTimeScale = value; }
		}

		public void TweenValue(float floatPercentage)
		{
			if (!ValidOnChangedTarget())
				return;

			var newPosition = Vector3.Lerp(m_StartPosition, m_TargetPosition, floatPercentage);

			m_OnChangedTarget.Invoke(newPosition);

			if (Math.Abs(floatPercentage - 1f) < Mathf.Epsilon)
			{
				m_OnCompleteTarget.Invoke();
			}
		}

		public void AddOnChangedCallback(UnityAction<Vector3> callback)
		{
			if (m_OnChangedTarget == null)
				m_OnChangedTarget = new PositionTweenCallback();

			m_OnChangedTarget.AddListener(callback);
		}

		public void AddOnCompleteCallback(UnityAction callback)
		{
			if (m_OnCompleteTarget == null)
				m_OnCompleteTarget = new UnityEvent();

			m_OnCompleteTarget.AddListener(callback);
		}

		public bool GetIgnoreTimescale()
		{
			return m_IgnoreTimeScale;
		}

		public float GetDuration()
		{
			return m_Duration;
		}

		public bool ValidOnChangedTarget()
		{
			return m_OnChangedTarget != null;
		}
	}

	internal struct RotationTween : ITweenValue
	{
		public class RotationTweenCallback : UnityEvent<Quaternion>
		{
		}

		private RotationTweenCallback m_OnChangedTarget;
		private UnityEvent m_OnCompleteTarget;
		private Quaternion m_StartRotation;
		private Quaternion m_TargetRotation;

		private float m_Duration;
		private bool m_IgnoreTimeScale;

		private AnimationCurve m_CustomAnimationCurve;


		public Quaternion startRotation
		{
			get { return m_StartRotation; }
			set { m_StartRotation = value; }
		}

		public Quaternion targetRotation
		{
			get { return m_TargetRotation; }
			set { m_TargetRotation = value; }
		}

		public float duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public bool ignoreTimeScale
		{
			get { return m_IgnoreTimeScale; }
			set { m_IgnoreTimeScale = value; }
		}

		public void TweenValue(float floatPercentage)
		{
			if (!ValidOnChangedTarget())
				return;

			var newRotation = Quaternion.Slerp(m_StartRotation, m_TargetRotation, floatPercentage);

			m_OnChangedTarget.Invoke(newRotation);

			if (m_OnCompleteTarget != null && Math.Abs(floatPercentage - 1f) < Mathf.Epsilon)
			{
				m_OnCompleteTarget.Invoke();
			}
		}

		public void AddOnChangedCallback(UnityAction<Quaternion> callback)
		{
			if (m_OnChangedTarget == null)
				m_OnChangedTarget = new RotationTweenCallback();

			m_OnChangedTarget.AddListener(callback);
		}

		public void AddOnCompleteCallback(UnityAction callback)
		{
			if (m_OnCompleteTarget == null)
				m_OnCompleteTarget = new UnityEvent();

			m_OnCompleteTarget.AddListener(callback);
		}

		public bool GetIgnoreTimescale()
		{
			return m_IgnoreTimeScale;
		}

		public float GetDuration()
		{
			return m_Duration;
		}

		public bool ValidOnChangedTarget()
		{
			return m_OnChangedTarget != null;
		}

		public bool ValidOnCompleteTarget()
		{
			return m_OnCompleteTarget != null;
		}
	}

	internal struct ScaleTween : ITweenValue
	{
		public class ScaleTweenCallback : UnityEvent<Vector3>
		{
		}

		private ScaleTweenCallback m_OnChangedTarget;
		private UnityEvent m_OnCompleteTarget;
		private Vector3 m_StartScale;
		private Vector3 m_TargetScale;

		private float m_Duration;
		private bool m_IgnoreTimeScale;

		private AnimationCurve m_CustomAnimationCurve;

		public Vector3 startScale
		{
			get { return m_StartScale; }
			set { m_StartScale = value; }
		}

		public Vector3 targetScale
		{
			get { return m_TargetScale; }
			set { m_TargetScale = value; }
		}

		public float duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public bool ignoreTimeScale
		{
			get { return m_IgnoreTimeScale; }
			set { m_IgnoreTimeScale = value; }
		}

		public void TweenValue(float floatPercentage)
		{
			if (!ValidOnChangedTarget())
				return;

			var newScale = Vector3.Lerp(m_StartScale, m_TargetScale, floatPercentage);

			m_OnChangedTarget.Invoke(newScale);

			if (m_OnCompleteTarget != null && Math.Abs(floatPercentage - 1f) < Mathf.Epsilon)
			{
				m_OnCompleteTarget.Invoke();
			}
		}

		public void AddOnChangedCallback(UnityAction<Vector3> callback)
		{
			if (m_OnChangedTarget == null)
				m_OnChangedTarget = new ScaleTweenCallback();

			m_OnChangedTarget.AddListener(callback);
		}

		public void AddOnCompleteCallback(UnityAction callback)
		{
			if (m_OnCompleteTarget == null)
				m_OnCompleteTarget = new UnityEvent();

			m_OnCompleteTarget.AddListener(callback);
		}

		public bool GetIgnoreTimescale()
		{
			return m_IgnoreTimeScale;
		}

		public float GetDuration()
		{
			return m_Duration;
		}

		public bool ValidOnChangedTarget()
		{
			return m_OnChangedTarget != null;
		}

		public bool ValidOnCompleteTarget()
		{
			return m_OnCompleteTarget != null;
		}
	}

	// Color tween class, receives the
	// TweenValue callback and then sets
	// the value on the target.
	internal struct ColorTween : ITweenValue
	{
		public enum ColorTweenMode
		{
			All,
			RGB,
			Alpha
		}

		public class ColorTweenCallback : UnityEvent<Color>
		{
		}

		private ColorTweenCallback m_OnChangedTarget;
		private UnityEvent m_OnCompleteTarget;
		private Color m_StartColor;
		private Color m_TargetColor;
		private ColorTweenMode m_TweenMode;

		private float m_Duration;
		private bool m_IgnoreTimeScale;

		private AnimationCurve m_CustomAnimationCurve;

		public Color startColor
		{
			get { return m_StartColor; }
			set { m_StartColor = value; }
		}

		public Color targetColor
		{
			get { return m_TargetColor; }
			set { m_TargetColor = value; }
		}

		public ColorTweenMode tweenMode
		{
			get { return m_TweenMode; }
			set { m_TweenMode = value; }
		}

		public float duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public bool ignoreTimeScale
		{
			get { return m_IgnoreTimeScale; }
			set { m_IgnoreTimeScale = value; }
		}

		public void TweenValue(float floatPercentage)
		{
			if (!ValidOnChangedTarget())
				return;

			var newColor = Color.Lerp(m_StartColor, m_TargetColor, floatPercentage);

			if (m_TweenMode == ColorTweenMode.Alpha)
			{
				newColor.r = m_StartColor.r;
				newColor.g = m_StartColor.g;
				newColor.b = m_StartColor.b;
			}
			else if (m_TweenMode == ColorTweenMode.RGB)
			{
				newColor.a = m_StartColor.a;
			}
			m_OnChangedTarget.Invoke(newColor);

			if ((m_OnCompleteTarget != null) && Math.Abs(floatPercentage - 1f) < Mathf.Epsilon)
			{
				m_OnCompleteTarget.Invoke();
			}
		}

		public void AddOnChangedCallback(UnityAction<Color> callback)
		{
			if (m_OnChangedTarget == null)
				m_OnChangedTarget = new ColorTweenCallback();

			m_OnChangedTarget.AddListener(callback);
		}

		public void AddOnCompleteCallback(UnityAction callback)
		{
			if (m_OnCompleteTarget == null)
				m_OnCompleteTarget = new UnityEvent();

			m_OnCompleteTarget.AddListener(callback);
		}

		public bool GetIgnoreTimescale()
		{
			return m_IgnoreTimeScale;
		}

		public float GetDuration()
		{
			return m_Duration;
		}

		public bool ValidOnChangedTarget()
		{
			return m_OnChangedTarget != null;
		}
	}

	// Float tween class, receives the
	// TweenValue callback and then sets
	// the value on the target.
	internal struct FloatTween : ITweenValue
	{
		public class FloatTweenCallback : UnityEvent<float> { }

		private FloatTweenCallback m_OnChangedTarget;
		private UnityEvent m_OnCompleteTarget;
		private float m_StartValue;
		private float m_TargetValue;

		private float m_Duration;
		private bool m_IgnoreTimeScale;

		public float startValue
		{
			get { return m_StartValue; }
			set { m_StartValue = value; }
		}

		public float targetValue
		{
			get { return m_TargetValue; }
			set { m_TargetValue = value; }
		}

		public float duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public bool ignoreTimeScale
		{
			get { return m_IgnoreTimeScale; }
			set { m_IgnoreTimeScale = value; }
		}

		public void TweenValue(float floatPercentage)
		{
			if (!ValidOnChangedTarget())
				return;

			var newValue = Mathf.Lerp(m_StartValue, m_TargetValue, floatPercentage);
			m_OnChangedTarget.Invoke(newValue);

			if (Math.Abs(floatPercentage - 1f) < Mathf.Epsilon)
			{
				m_OnCompleteTarget.Invoke();
			}
		}

		public void AddOnChangedCallback(UnityAction<float> callback)
		{
			if (m_OnChangedTarget == null)
				m_OnChangedTarget = new FloatTweenCallback();

			m_OnChangedTarget.AddListener(callback);
		}

		public void AddOnCompleteCallback(UnityAction callback)
		{
			if (m_OnCompleteTarget == null)
				m_OnCompleteTarget = new UnityEvent();

			m_OnCompleteTarget.AddListener(callback);
		}

		public bool GetIgnoreTimescale()
		{
			return m_IgnoreTimeScale;
		}

		public float GetDuration()
		{
			return m_Duration;
		}

		public bool ValidOnChangedTarget()
		{
			return false;
//			return m_OnChangedTarget != null;
		}
	}

	public enum EaseType
	{
		Linear,
		EaseInSine,
		EaseOutSine,
		EaseInOutSine,
		EaseInQuad,
		EaseOutQuad,
		EaseInOutQuad,
		EaseInCubic,
		EaseOutCubic,
		EaseInOutCubic,
	}

	public enum TweenLoopType
	{
		Clamp,
		Loop,
		PingPong,
	}

	// Tween runner, executes the given tween.
	// The coroutine will live within the given 
	// behaviour container.
	internal class TweenRunner<T> where T : struct, ITweenValue // changed from internal to public
	{
		protected MonoBehaviour m_CoroutineContainer;
		protected IEnumerator m_Tween;
		protected EaseType m_EaseType;
		protected TweenLoopType m_TweenLoopType = TweenLoopType.Clamp;

		protected bool m_Paused;

		// utility function for starting the tween
//		private static IEnumerator Start(T tweenInfo)
		private IEnumerator Start(T tweenInfo)
		{
			if (!tweenInfo.ValidOnChangedTarget())
				yield break;

			var elapsedTime = 0.0f;
			while (elapsedTime < tweenInfo.duration)
			{
				if (m_Paused)
				{
					yield return null;
					continue;
				}

				elapsedTime += tweenInfo.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
				var t = Mathf.Clamp01(elapsedTime / tweenInfo.duration);

				switch (m_EaseType)
				{
					case EaseType.Linear:
						break;
					case EaseType.EaseInSine:
						t = -Mathf.Cos(t * 0.5f * Mathf.PI) + 1f;
						break;
					case EaseType.EaseOutSine:
						t = Mathf.Sin(t * 0.5f * Mathf.PI);
						break;
					case EaseType.EaseInOutSine:
						t = -0.5f * (Mathf.Cos(t * Mathf.PI) - 1f);
						break;
					case EaseType.EaseInQuad:
						t = t * t;
						break;
					case EaseType.EaseOutQuad:
						t = -t * (t - 2f);
						break;
					case EaseType.EaseInOutQuad:
						t *= 2f;
						if (t < 1f)
							t = t * t * 0.5f;
						else
						{
							t -= 1f;
							t = -0.5f * (t * (t - 2f) - 1f);
						}
						break;
					case EaseType.EaseInCubic:
						t = t * t * t;
						break;
					case EaseType.EaseOutCubic:
						t -= 1f;
						t = t * t * t + 1f;
						break;
					case EaseType.EaseInOutCubic:
						t *= 2f;
						if (t < 1)
							t = 0.5f * t * t * t;
						t -= 2f;
						t = 0.5f * (t * t * t + 2f);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				tweenInfo.TweenValue(t);

				yield return null;
			}
			tweenInfo.TweenValue(1.0f);
		}

		public void Init(MonoBehaviour coroutineContainer)
		{
			m_CoroutineContainer = coroutineContainer;
		}

		public void StartTween( T info, EaseType easeType = EaseType.Linear, bool startImmedietely = true,
			TweenLoopType tweenLoopType = TweenLoopType.Clamp )
		{
			if (m_CoroutineContainer == null)
			{
				Debug.LogWarning("Coroutine container not configured... did you forget to call Init?");
				return;
			}

			m_Paused = false;

			m_EaseType = easeType;

			if (m_Tween != null)
			{
				m_CoroutineContainer.StopCoroutine(m_Tween);
				m_Tween = null;
			}

			if (!m_CoroutineContainer.gameObject.activeInHierarchy)
			{
				info.TweenValue(1.0f);
				return;
			}

			m_TweenLoopType = tweenLoopType;

			m_Tween = Start(info);

			if (startImmedietely)
				m_CoroutineContainer.StartCoroutine(m_Tween);
		}

		public void PlayTween()
		{
			if (m_Tween != null && m_CoroutineContainer != null && m_CoroutineContainer.gameObject.activeInHierarchy)
			{
				m_CoroutineContainer.StartCoroutine(m_Tween);
			}
		}

		public void StopTween()
		{
			if (m_Tween != null)
			{
				m_CoroutineContainer.StopCoroutine(m_Tween);
			}
		}

		public void PauseTween()
		{
			m_Paused = true;
		}

		public void ResumeTween()
		{
			m_Paused = false;
		}
	}
}
