using UnityEngine;
using System;
using System.Threading;
using System.Collections.Concurrent;

/// <summary>
/// Klasa umożliwia asynchroniczne wywoływanie funkcji i wykonywanie kodu na osobnym wątku.
/// Metoda RequestData służy do wysyłania żądań wykonania funkcji z parametrami na osobnym wątku,
/// a metoda Update wykonuje wywołania funkcji z kolejkującej się w kolejce wywołań. Klasa jest użyteczna,
/// gdy potrzeba wykonywać długotrwałe operacje, takie jak generowanie danych lub pobieranie plików,
/// bez blokowania wątku głównego aplikacji.
/// </summary>

public class ThreadedDataRequester : MonoBehaviour
{
    private static ThreadedDataRequester _instance;
    private readonly ConcurrentQueue<ThreadInfo> m_DataQueue = new ConcurrentQueue<ThreadInfo>(); 

    private void Awake()
    {
        _instance = FindObjectOfType<ThreadedDataRequester>();
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        new Thread(() => _instance.DataThread(generateData, callback)).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        m_DataQueue.Enqueue(new ThreadInfo(callback, data));
    }

    private void Update()
    {
        while (m_DataQueue.TryDequeue(out var threadInfo))
        {
            threadInfo.callback(threadInfo.parameter);
        }
    }

    private readonly struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}