using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Threads
{
    public class SentenceProcessor
    {
        public readonly BlockingQueue<string> Sentences = new();
        public event EventHandler<SentenceProcessedArg> SentenceProcessed;
        private bool _Stop;
        public Guid WorkerId;

        public SentenceProcessor()
        {
            WorkerId = Guid.NewGuid();
        }

        //Thread nesnesinin başlatılma aşaması
        public void Start()
        {
            Console.WriteLine($"Worker {WorkerId.ToString("D")} is starting");

            OnStart();
        }

        public void Stop()
        {
            _Stop = true;
        }



        private void OnStart()
        {
            while (!_Stop)
            {
                // queue dan item çekilmesi ve işlenmesi
                var task = Sentences.Dequeue();
                Console.WriteLine($"Task dequeued -> {WorkerId}");
                if (task == null) return;
                var words = task.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                OnSentenceProcessed(new SentenceProcessedArg { Words = words.ToList(), Sentence = task });

            }
            Console.WriteLine($"Worker {WorkerId.ToString("D")} is stopped");
        }


        protected virtual void OnSentenceProcessed(SentenceProcessedArg e)
        {
            // cümlenin işlenmesinin ardından event in işlenen cümleler ile gönderilmesi
            SentenceProcessed?.Invoke(this, e);
        }
        // alt thread queue larına cumle eklenmesi
        public void AddSentence(string sentence)
        {
            Sentences.Enqueue(sentence);
            Console.WriteLine($"Task added -> {WorkerId}");
        }
    }
}

