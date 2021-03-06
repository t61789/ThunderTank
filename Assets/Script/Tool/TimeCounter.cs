﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Tool
{
    public abstract class Counter
    {
        protected float _TimeCountStart;

        protected float _TimeLimit;

        protected Counter(float timeLimit)
        {
            _TimeLimit = timeLimit;
            _TimeCountStart = Time.time;
        }

        /// <summary>
        ///     当前计时与目标时间的插值，经过clamp处理
        /// </summary>
        public float Interpolant =>
            Tools.InLerp(0, _TimeLimit, TimeCount);

        /// <summary>
        ///     当前计时与目标时间的插值，未经clamp处理
        /// </summary>
        public float InterpolantUc =>
            Tools.InLerpUc(0, _TimeLimit, TimeCount);

        /// <summary>
        ///     当前已经记录了多少时间
        /// </summary>
        public abstract float TimeCount { get; }

        /// <summary>
        ///     指示是否已经完成计时
        /// </summary>
        public abstract bool Completed { get; }

        /// <summary>
        ///     计时上限
        /// </summary>
        public float TimeLimit => _TimeLimit;

        /// <summary>
        /// 重新计数
        /// </summary>
        /// <param name="timeLimit">新的计数时限，若为-1则不做改动</param>
        public abstract void Recount(float timeLimit = -1);

        /// <summary>
        /// 根据指定的factor设置当前计数器的计时起点
        /// </summary>
        /// <param name="interpolant"></param>
        public abstract void SetCountValue(float factor);
    }

    public class SimpleCounter : Counter
    {
        public SimpleCounter(float timeLimit, bool countAtStart = true) : base(timeLimit)
        {
            if (countAtStart) return;
            _TimeCountStart -= _TimeLimit;
        }

        public override float TimeCount =>
            Time.time - _TimeCountStart;

        public override bool Completed => Time.time >= _TimeCountStart + _TimeLimit;

        public override void Recount(float timeLimit = -1)
        {
            _TimeLimit = timeLimit == -1 ? _TimeLimit : timeLimit;
            _TimeCountStart = Time.time;
        }

        public override void SetCountValue(float factor)
        {
            _TimeCountStart = Time.time - factor * TimeLimit;
        }

        /// <summary>
        ///     立即完成计时
        /// </summary>
        /// <returns></returns>
        public SimpleCounter Complete()
        {
            _TimeCountStart = Time.time - _TimeLimit;
            return this;
        }
    }

    public class SimpleCounterQueue
    {
        public event Action<int> OnStageCompleted;

        private readonly float[] _TimeLimitQueue;

        public SimpleCounter Counter { get; }
        public int CurStage { private set; get; }

        public SimpleCounterQueue(MonoBehaviour host, SimpleCounter counter, float[] timeLimitQueue)
        {
            Counter = counter;
            _TimeLimitQueue = timeLimitQueue;
            host.StartCoroutine(Count());
        }

        public void Play(int stage = 0)
        {
            CurStage = stage;
            Counter.Recount(_TimeLimitQueue[CurStage]);
        }

        public void Stop()
        {
            CurStage = _TimeLimitQueue.Length;
        }

        private IEnumerator Count()
        {
            while (true)
            {
                yield return null;
                if (CurStage == _TimeLimitQueue.Length || !Counter.Completed) continue;
                OnStageCompleted?.Invoke(CurStage);
                CurStage++;
                if (CurStage == _TimeLimitQueue.Length) continue;
                Counter.Recount(_TimeLimitQueue[CurStage]);
            }
        }
    }

    /// <summary>
    /// 半自动计时器，使用提供的Update和FixedUpdate实现自动执行回调函数，而非协程<br/>
    /// 用法大体与自动计时器相同，但对性能更友好，适用于大量简单物体的计时
    /// </summary>
    public class SemiAutoCounter : Counter
    {
        private float _CountPauseSave;
        private bool _HasExcutedCompleteCallBack;
        private Action _OnCompleteCallBack;
        private bool _Running = true;

        /// <summary>
        /// </summary>
        /// <param name="parent">协程附着对象</param>
        /// <param name="timeLimit"></param>
        /// <param name="countAtStart"></param>
        public SemiAutoCounter(float timeLimit) : base(timeLimit)
        {
            _TimeCountStart = Time.time;
        }

        public override float TimeCount
        {
            get
            {
                if (!_Running)
                    return _CountPauseSave;
                return Time.time - _TimeCountStart;
            }
        }

        public override bool Completed => _HasExcutedCompleteCallBack;

        public override void Recount(float timeLimit = -1)
        {
            _TimeLimit = timeLimit == -1 ? _TimeLimit : timeLimit;
            _TimeCountStart = Time.time;
            _HasExcutedCompleteCallBack = false;
            _CountPauseSave = 0;
        }

        /// <summary>
        ///     立即完成计时
        /// </summary>
        /// <param name="callback">是否调用完成回调函数</param>
        /// <returns></returns>
        public SemiAutoCounter Complete(bool callback = true)
        {
            _TimeCountStart = Time.time - _TimeLimit;
            if (callback)
                _OnCompleteCallBack?.Invoke();
            _HasExcutedCompleteCallBack = true;
            _CountPauseSave = _TimeLimit;
            return this;
        }

        /// <summary>
        ///     启用自动计时后调用该回调函数
        /// </summary>
        /// <param name="callBack">回调函数</param>
        /// <returns></returns>
        public SemiAutoCounter OnComplete(Action callBack)
        {
            _OnCompleteCallBack = callBack;
            return this;
        }

        /// <summary>
        ///     恢复自动计时，如果当前正在自动计时，则重置计数
        /// </summary>
        /// <returns></returns>
        public SemiAutoCounter Resume()
        {
            _Running = true;
            _TimeCountStart = Time.time - _CountPauseSave;
            return this;
        }

        /// <summary>
        ///     暂停自动计时
        /// </summary>
        /// <returns></returns>
        public SemiAutoCounter Pause()
        {
            _Running = false;
            _CountPauseSave = Time.time - _TimeCountStart;
            return this;
        }

        public void Update()
        {
            if (_HasExcutedCompleteCallBack || !_Running || Time.time <= _TimeCountStart + _TimeLimit) return;
            _HasExcutedCompleteCallBack = true;
            _OnCompleteCallBack?.Invoke();
        }

        public void FixedUpdate()
        {
            if (_HasExcutedCompleteCallBack || !_Running || Time.time <= _TimeCountStart + _TimeLimit) return;
            _HasExcutedCompleteCallBack = true;
            _OnCompleteCallBack?.Invoke();
        }

        public override void SetCountValue(float factor)
        {
            if (_Running)
                _TimeCountStart = Time.time - factor * TimeLimit;
            else
                _CountPauseSave = TimeLimit * factor;

            if (factor <= 0)
                _HasExcutedCompleteCallBack = false;
        }
    }

    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    public class SemiAutoCounterHub
    {
        private readonly List<SemiAutoCounter> _Counters;

        public SemiAutoCounterHub(params SemiAutoCounter[] counters)
        {
            _Counters = new List<SemiAutoCounter>(counters);
        }

        public void Update()
        {
            for (int i = 0; i < _Counters.Count; i++)
                _Counters[i].Update();
        }

        public void FixedUpdate()
        {
            for (int i = 0; i < _Counters.Count; i++)
                _Counters[i].FixedUpdate();
        }

        public void AddCounter(SemiAutoCounter counter)
        {
            _Counters.Add(counter);
        }

        public void RemoveCounter(SemiAutoCounter counter)
        {
            _Counters.Remove(counter);
        }
    }

    public class AutoCounter : Counter
    {
        private float _CountPauseSave;
        private bool _HasExcutedCompleteCallBack;
        private Action _OnCompleteCallBack;
        private bool _Running = true;

        /// <summary>
        /// </summary>
        /// <param name="parent">协程附着对象</param>
        /// <param name="timeLimit"></param>
        public AutoCounter(MonoBehaviour parent, float timeLimit) : base(timeLimit)
        {
            _TimeCountStart = Time.time;
            parent.StartCoroutine(Count());
        }

        public override float TimeCount
        {
            get
            {
                if (!_Running)
                    return _CountPauseSave;
                return Time.time - _TimeCountStart;
            }
        }

        public override bool Completed => _HasExcutedCompleteCallBack;

        public override void Recount(float timeLimit = -1)
        {
            _TimeLimit = timeLimit == -1 ? _TimeLimit : timeLimit;
            _TimeCountStart = Time.time;
            _HasExcutedCompleteCallBack = false;
            _CountPauseSave = 0;
        }

        public override void SetCountValue(float factor)
        {
            if (_Running)
                _TimeCountStart = Time.time - factor * TimeLimit;
            else
                _CountPauseSave = TimeLimit * factor;

            if (factor <= 0)
                _HasExcutedCompleteCallBack = false;
        }

        public AutoCounter Complete(bool callback = true)
        {
            _TimeCountStart = Time.time - _TimeLimit;
            if (callback)
                _OnCompleteCallBack?.Invoke();
            _HasExcutedCompleteCallBack = true;
            _CountPauseSave = _TimeLimit;
            return this;
        }

        /// <summary>
        ///     启用自动计时后调用该回调函数
        /// </summary>
        /// <param name="callBack">回调函数</param>
        /// <returns></returns>
        public AutoCounter OnComplete(Action callBack)
        {
            _OnCompleteCallBack = callBack;
            return this;
        }

        /// <summary>
        ///     恢复自动计时，如果当前正在自动计时，则重置计数
        /// </summary>
        /// <returns></returns>
        public AutoCounter Resume()
        {
            _Running = true;
            _TimeCountStart = Time.time - _CountPauseSave;
            return this;
        }

        /// <summary>
        ///     暂停自动计时
        /// </summary>
        /// <returns></returns>
        public AutoCounter Pause()
        {
            _Running = false;
            _CountPauseSave = Time.time - _TimeCountStart;
            return this;
        }

        private IEnumerator Count()
        {
            while (true)
            {
                if (!_HasExcutedCompleteCallBack && _Running && Time.time >= _TimeCountStart + _TimeLimit)
                {
                    _HasExcutedCompleteCallBack = true;
                    _OnCompleteCallBack?.Invoke();
                }

                yield return null;
            }
        }
    }
}
